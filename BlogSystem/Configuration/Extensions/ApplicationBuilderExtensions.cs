using Asp.Versioning.ApiExplorer;
using BlogSystem.Middlewares;

namespace BlogSystem.Configuration.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder Configure(
        this WebApplication app)
    {
        app.UseExceptionHandlerMiddleware();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.UseSwaggerSetup();
        app.MapControllers();

        return app;
    }

    private static WebApplication UseSwaggerSetup(
        this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            IApiVersionDescriptionProvider provider = app.Services
                .GetRequiredService<IApiVersionDescriptionProvider>();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (ApiVersionDescription description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        description.GroupName.ToUpperInvariant());
                }
            });
        }

        return app;
    }

    private static IApplicationBuilder UseExceptionHandlerMiddleware(
        this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlerMiddleware>();

        return app;
    }
}
