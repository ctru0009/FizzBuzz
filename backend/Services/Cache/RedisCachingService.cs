using backend.Interfaces.Cache;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace backend.Services.Cache
{
    public class RedisCachingService : IRedisCachingService
    {
        private readonly IDistributedCache _redis;

        public RedisCachingService(IDistributedCache redis)
        {
            _redis = redis;
        }
        public List<int>? GetData(string key)
        {
            var data = _redis.GetString(key);
            if (data is null)
            {
                return default;
            }

            return JsonSerializer.Deserialize<List<int>>(data);
        }

        public bool IsExist(string key)
        {
            var data = _redis.GetString(key);
            return data is not null;
        }

        public void RPushData(string key, int data)
        {
            List<int>? deserializedData = GetData(key);
            if (deserializedData is not null)
            {
                deserializedData.Add(data);
            }

            SetData(key, deserializedData!);
        }

        public void SetData(string key, List<int> data)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            _redis.SetString(key, JsonSerializer.Serialize<List<int>>(data), options);
        }
    }
}
