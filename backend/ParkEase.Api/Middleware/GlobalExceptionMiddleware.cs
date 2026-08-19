using System.Net;
using System.Text.Json;

namespace ParkEase.Api.Middleware
{
    // Catches any unhandled exception anywhere in the request pipeline and converts it
    // into a consistent JSON error shape instead of leaking a raw stack trace to the client.
    // This directly satisfies the caselet's "Consistent API error response format" requirement.
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var errorResponse = new
                {
                    statusCode = 500,
                    message = "An unexpected error occurred. Please try again later.",
                    // CorrelationId lets you match a support ticket / bug report back to
                    // the exact server log entry above, without exposing internals to the client.
                    correlationId = context.TraceIdentifier
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            }
        }
    }
}