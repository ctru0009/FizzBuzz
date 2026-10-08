using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace backend.Health
{
    public class RedisHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                };
                await cache.SetStringAsync("health:ready-probe", "ok", options, cancellationToken);
                var value = await cache.GetStringAsync("health:ready-probe", cancellationToken);
                return string.Equals(value, "ok", StringComparison.Ordinal)
                    ? HealthCheckResult.Healthy("Redis is reachable.")
                    : HealthCheckResult.Unhealthy("Redis round trip returned an unexpected value.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Redis check failed.", ex);
            }
        }
    }
}
