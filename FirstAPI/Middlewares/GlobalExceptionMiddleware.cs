using System.Net;
using System.Text.Json;
using FirstAPI.Exceptions;

namespace FirstAPI.Middlewares
{
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

                // Intercept 401/403 that weren't thrown as exceptions
                // (e.g. missing token — JWT middleware sets status but doesn't throw)
                if (!context.Response.HasStarted)
                {
                    if (context.Response.StatusCode == 401)
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            statusCode = 401,
                            errorCode  = "UNAUTHORIZED",
                            message    = "Authentication required. Please provide a valid Bearer token."
                        }));
                    }
                    else if (context.Response.StatusCode == 403)
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        {
                            statusCode = 403,
                            errorCode  = "FORBIDDEN",
                            message    = "You do not have permission to access this resource."
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted) return;

            var (statusCode, errorCode) = exception switch
            {
                UnAuthorizedException         => (HttpStatusCode.Unauthorized,          "UNAUTHORIZED"),
                EntityNotFoundException       => (HttpStatusCode.NotFound,              "NOT_FOUND"),
                DuplicateEntityException      => (HttpStatusCode.Conflict,              "DUPLICATE"),
                Exceptions.ValidationException => (HttpStatusCode.BadRequest,           "VALIDATION_ERROR"),
                UnableToCreateEntityException => (HttpStatusCode.InternalServerError,   "CREATE_FAILED"),
                _                             => (HttpStatusCode.InternalServerError,   "INTERNAL_ERROR")
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = (int)statusCode;

            var response = new
            {
                statusCode = (int)statusCode,
                errorCode,
                message    = exception.Message
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
        }
    }
}
