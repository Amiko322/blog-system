using BlogSystem.Configuration.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddConfiguration(
    builder.Configuration);

WebApplication app = builder.Build();

app.Configure();

app.Run();
