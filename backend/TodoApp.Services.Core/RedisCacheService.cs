using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using TodoApp.Services.Core.Contracts;

namespace TodoApp.Services.Core;

public class RedisCacheService: IRedisCacheService
{
    private readonly IDistributedCache _distributedCache;
    
    public RedisCacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetData<T>(string key)
    {
        string? data = await _distributedCache.GetStringAsync(key);

        if (string.IsNullOrEmpty(data))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetData<T>(string key, T value)
    {
        DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };
        
        await _distributedCache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
    }

    public async Task RemoveData(string key)
    {
        await _distributedCache.RemoveAsync(key);
    }
}