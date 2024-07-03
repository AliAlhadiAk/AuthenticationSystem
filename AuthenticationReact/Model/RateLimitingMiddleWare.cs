using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly int _limit;
    private readonly TimeSpan _interval;

    public RateLimitMiddleware(RequestDelegate next, IMemoryCache cache, int limit, TimeSpan interval)
    {
        _next = next;
        _cache = cache;
        _limit = limit;
        _interval = interval;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress.ToString();
        var cacheKey = $"{ipAddress}_{DateTime.UtcNow.ToString("yyyyMMddHHmm")}";

        // Check if the request limit has been reached
        if (!_cache.TryGetValue(cacheKey, out int count))
        {
            count = 0;
        }

        if (count >= _limit)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        // Increment request count
        count++;
        _cache.Set(cacheKey, count, _interval);

        // Call the next middleware in the pipeline
        await _next(context);
    }
}
