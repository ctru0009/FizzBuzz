using backend.Interfaces.Cache;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace backend.Services.Cache
{
    public sealed class RedisCachingService : IRedisCachingService
    {
        private readonly IDistributedCache _cache;

        public RedisCachingService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<IReadOnlyList<int>?> GetUsedNumbersAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            var json = await _cache.GetStringAsync(UsedNumbersKey(sessionId), cancellationToken);
            if (json is null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<List<int>>(json);
        }

        public async Task AddUsedNumberAsync(int sessionId, int number, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            var numbers = (await GetUsedNumbersAsync(sessionId, cancellationToken) ?? []).ToList();
            numbers.Add(number);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
            };
            var json = JsonSerializer.Serialize(numbers);
            await _cache.SetStringAsync(UsedNumbersKey(sessionId), json, options, cancellationToken);
        }

        private static string UsedNumbersKey(int sessionId) => $"session:{sessionId}:used";
    }
}
