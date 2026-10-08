using backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace backend.Health
{
    public class PostgresHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BackendAppDbContext>();
                var reachable = await db.Database.CanConnectAsync(cancellationToken);
                return reachable
                    ? HealthCheckResult.Healthy("Postgres is reachable.")
                    : HealthCheckResult.Unhealthy("Postgres refused the connection.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Postgres check failed.", ex);
            }
        }
    }
}
