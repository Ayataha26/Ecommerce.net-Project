using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;

namespace MarketPlaceApi.Middleware
{
    public class AdminAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminAuthenticationMiddleware> _logger;

        public AdminAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<AdminAuthenticationMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/api/admin"))
            {
                if (!context.Request.Headers.TryGetValue("X-Admin-Email", out var email) ||
                    !context.Request.Headers.TryGetValue("X-Admin-Password", out var password))
                {
                    _logger.LogWarning("Admin authentication failed: Missing admin credentials for request to {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    var errorResponse = new { Message = "Missing admin credentials." };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                    return;
                }

                var adminEmail = _configuration["AdminCredentials:Email"];
                var adminPassword = _configuration["AdminCredentials:Password"];

                if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
                {
                    _logger.LogError("Admin credentials are not configured in appsettings.json.");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    var errorResponse = new { Message = "Server configuration error: Admin credentials not found." };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                    return;
                }

                if (email != adminEmail || password != adminPassword)
                {
                    _logger.LogWarning("Admin authentication failed: Invalid credentials for request to {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    var errorResponse = new { Message = "Invalid admin credentials." };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                    return;
                }

                _logger.LogInformation("Admin authenticated successfully for request to {Path}", context.Request.Path);
            }

            await _next(context);
        }
    }
}