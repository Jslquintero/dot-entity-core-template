using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Template.Api.Middlewares
{
    public class ApplicationIdentityMiddleware
    {
        private readonly RequestDelegate _next;

        public ApplicationIdentityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add custom application identity logic here
            // For example: set correlation IDs, custom headers, request tracking, etc.

            // Example: Add a custom header to identify the application
            context.Response.Headers["X-Application"] = "Template.Api";

            await _next(context);
        }
    }

    public static class ApplicationIdentityMiddlewareExtensions
    {
        public static IApplicationBuilder UseApplicationIdentity(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApplicationIdentityMiddleware>();
        }
    }
}