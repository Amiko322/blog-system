using BlogSystem.Contracts;
using System.Security.Authentication;

namespace BlogSystem.Middlewares;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next.Invoke(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionMessageAsync(context, ex);
        }
    }

    private static async Task HandleExceptionMessageAsync(HttpContext context, Exception exception)
    {
        ExceptionResponse response = exception switch
        {
            KeyNotFoundException _ => new()
            {
                StatusCode = StatusCodes.Status404NotFound,
                Message = "The requested resource could not be found.",
            },
            InvalidOperationException _ => new()
            {
                StatusCode = StatusCodes.Status409Conflict,
                Message = "A conflict occurred while processing your request.",
            },
            AuthenticationException _ => new()
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "Authentication failed. Please check your credentials and try again.",
            },
            _ => new()
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "Internal server error.",
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;

        await context.Response.WriteAsJsonAsync(response);
    }
}
