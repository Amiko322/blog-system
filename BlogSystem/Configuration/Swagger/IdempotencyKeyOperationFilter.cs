using IdempotentAPI.Filters;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BlogSystem.Configuration.Swagger;

public class IdempotencyKeyOperationFilter : IOperationFilter
{
    // Добавляет заголовок идемпотентности для методов c атрибутом [Idempotent]
    // Вызывается для каждого метода действия
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        operation.Parameters ??= [];

        if (!context.MethodInfo.GetCustomAttributes(true)
            .OfType<IdempotentAttribute>()
            .Any())
            return;

        // Добавляем обязательный заголовок IdempotencyKey
        operation.Parameters.Add(new OpenApiParameter()
        {
            Name = "IdempotencyKey",
            In = ParameterLocation.Header,
            Required = true,
            Schema = new OpenApiSchema()
            {
                Type = "string",
                Format = "uuid",
            },
        });
    }
}
