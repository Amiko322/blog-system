using Asp.Versioning;
using BlogSystem.Configuration.Options;
using BlogSystem.Configuration.Swagger;
using BlogSystem.Contracts;
using BlogSystem.Models;
using BlogSystem.RabbitMq.Consumers;
using BlogSystem.RabbitMq.Producers;
using BlogSystem.RabbitMq.Services;
using BlogSystem.Services;
using IdempotentAPI.Cache.DistributedCache.Extensions.DependencyInjection;
using IdempotentAPI.Core;
using IdempotentAPI.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

namespace BlogSystem.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddSwaggerSetup();
        services.AddFixedRateLimiter();

        services.AddDbContext<AppDbContext>(builder =>
            builder.UseNpgsql(
                configuration.GetConnectionString("Postgres")));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        services.AddSingleton<IConnectionMultiplexer>(_ => 
            ConnectionMultiplexer.Connect(
                configuration.GetConnectionString("Redis")!));

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPostService, PostService>();

        services.AddScoped<ITokenFactory, TokenFactory>();

        services.Configure<TokenOptions>(configuration
            .GetSection(nameof(TokenOptions)));

        services.AddJwtAuthentication(configuration);

        services.AddIdempotentAPI(new IdempotencyOptions());
        services.AddIdempotentAPIUsingDistributedCache();

        services.AddScoped(typeof(IDataShaper<>), typeof(DataShaper<>));

        services.AddRabbitMqSetup(configuration);

        return services;
    }

    private static IServiceCollection AddRabbitMqSetup(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(configuration
            .GetSection(nameof(RabbitMqOptions)));

        services.AddSingleton<IApiMessageConsumer, ApiMessageConsumer>();
        services.AddScoped<IMessageProcessingService, MessageProcessingService>();
        services.AddSingleton<IIdempotencyService, IdempotencyService>();

        services.AddSingleton<IRabbitMqProducer, RabbitMqProducer>();

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        TokenOptions? tokenOptions = configuration
            .GetSection(nameof(TokenOptions))
            .Get<TokenOptions>();

        SymmetricSecurityKey key = new(
            Encoding.UTF8.GetBytes(tokenOptions?.Key!));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidIssuer = tokenOptions?.Issuer,
                    ValidateAudience = true,
                    ValidAudience = tokenOptions?.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                };
            });

        return services;
    }

    private static IServiceCollection AddSwaggerSetup(
        this IServiceCollection services)
    {
        services.ConfigureOptions<ConfigureSwaggerOptions>();
        services
            .AddSwaggerGen(options =>
            {
                // Добавляем поддержку идемпотентности в документации
                // Вызывается для каждого метода действия (endpoint'а)
                options.OperationFilter<IdempotencyKeyOperationFilter>();

                // Добавляем схему безопасности: JWT Bearer-аутентификацию в Swagger
                options.AddSecurityDefinition(
                    JwtBearerDefaults.AuthenticationScheme,
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = JwtBearerDefaults.AuthenticationScheme,
                    });

                // Используем схему безопасности из AddSecurityDefinition
                // Требуем авторизацию для защищённых ([Authorize]) операций
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme()
                        {
                            Reference = new OpenApiReference()
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = JwtBearerDefaults.AuthenticationScheme,
                            },
                        },
                        Array.Empty<string>()
                    },
                });
            })
            .AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1);
                options.ReportApiVersions = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader());
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }

    private static IServiceCollection AddFixedRateLimiter(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // Общий лимитер на основе 'фиксированного окна' для всех входящих запросов
            options.GlobalLimiter = PartitionedRateLimiter
                .Create<HttpContext, string>(context =>
                {
                    string? teacherId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        teacherId
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? context.Request.Headers.Host.ToString(),
                        _ => new FixedWindowRateLimiterOptions()
                        {
                            PermitLimit = 10,
                            QueueLimit = 0,
                            Window = TimeSpan.FromSeconds(30),
                            AutoReplenishment = true,
                        });
                });

            // Этот обработчик вызывается, когда клиент превысил лимит запросов
            options.OnRejected = async (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    // RetryAfter - заголовок: через какое время можно повторить запрос
                    // X-Limit-Remaining - заголовок: количество доступных запросов
                    context.HttpContext.Response.Headers.Append("X-Limit-Remaining", "0");
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds)
                            .ToString(NumberFormatInfo.InvariantInfo);
                }

                ExceptionResponse response = new()
                {
                    StatusCode = StatusCodes.Status429TooManyRequests,
                    Message = "Too many requests. Please try again later.",
                };

                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.StatusCode = response.StatusCode;

                await context.HttpContext.Response.WriteAsJsonAsync(
                    response,
                    CancellationToken.None);
            };
        });

        return services;
    }
}
