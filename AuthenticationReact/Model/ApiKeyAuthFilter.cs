using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace AuthenticationReact.Model
{
    public class ApiKeyAuthFilter
    {
        private readonly IConfiguration _configuration;
        public ApiKeyAuthFilter(IConfiguration configuration)

        {
            _configuration = configuration;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var providedApiKwy = context.HttpContext.Request.Headers[AuthConfig.ApiKeyHeader].FirstOrDefault();
            var isValid = IsValidApiKey(providedApiKwy);
            if (!isValid)
            {
                context.Result = new UnauthorizedObjectResult("Invalid Authentication");
            }
        }

        private bool IsValidApiKey(string providedApiKey)
        {
            if (string.IsNullOrEmpty(providedApiKey))
                return false;
            var validApikey = _configuration.GetValue<string>(AuthConfig.AuthSection);

            return string.Equals(validApikey, providedApiKey, StringComparison.Ordinal);
        }
    }
}
