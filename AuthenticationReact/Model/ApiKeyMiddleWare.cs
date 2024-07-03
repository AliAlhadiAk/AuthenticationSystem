using AuthenticationReact.Model;

namespace Asp.NetBooks.NewFolder
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private IConfiguration _configuration;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var providedApiKey = context.Request.Headers[AuthConfig.ApiKeyHeader].FirstOrDefault();
            var isValid = IsValidApiKey(providedApiKey);

            if (!isValid)
            {
                await GenerateResponse(context, 401, "InvalidAuthentication");
            }
            await _next(context);
        }

        private bool IsValidApiKey(string providedApiKey)
        {
            if (string.IsNullOrEmpty(providedApiKey))
                return false;
            var validApikey = _configuration.GetValue<string>(AuthConfig.AuthSection);

            return string.Equals(validApikey, providedApiKey, StringComparison.Ordinal);
        }
        private static async Task GenerateResponse(HttpContext context, int status, string error)
        {
            context.Response.StatusCode = status;
            await context.Response.WriteAsync(error);

        }
    }
}
