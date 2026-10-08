using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace backend.Middleware
{
    public class ProblemDetailsExceptionMiddleware(RequestDelegate next, ILogger<ProblemDetailsExceptionMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ProblemDetailsExceptionMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AntiforgeryValidationException ex)
            {
                _logger.LogWarning(ex, "Antiforgery validation failed for {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Invalid antiforgery token.");
            }
            catch (BadHttpRequestException ex)
            {
                _logger.LogWarning(ex, "Bad request for {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteProblemAsync(context, (HttpStatusCode)ex.StatusCode, "The request was invalid.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteProblemAsync(context, HttpStatusCode.InternalServerError, "The request failed. Check the server logs for details.");
            }
        }

        private static async Task WriteProblemAsync(HttpContext context, HttpStatusCode status, string detail)
        {
            if (context.Response.HasStarted)
            {
                throw new InvalidOperationException("Cannot write a problem response after the response has started.");
            }

            context.Response.Clear();
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetails
            {
                Status = (int)status,
                Title = status == HttpStatusCode.InternalServerError ? "An error occurred." : "The request was invalid.",
                Detail = detail,
                Instance = context.Request.Path
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
