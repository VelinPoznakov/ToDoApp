using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TodoApp.Services.Core.Contracts;

using static TodoApp.GCommon.ApplicationConstants;
using static TodoApp.GCommon.ErrorMessages;

namespace TodoApp.Services.Core;

public class RedisCacheService: IRedisCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IConnectionMultiplexer _redis;
    
    public RedisCacheService(
        IDistributedCache distributedCache,
        ILogger<RedisCacheService> logger,
        IConnectionMultiplexer redis)
    {
        _distributedCache = distributedCache;
        _logger = logger;
        _redis = redis;
    }

    public async Task<T?> GetData<T>(string key)
    {
        try
        {
            string? data = await _distributedCache.GetStringAsync(key);

            if (string.IsNullOrEmpty(data))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, RedisError);
        }

        return default;
    }

    public async Task<bool> SetData<T>(string key, T value)
    {
        DistributedCacheEntryOptions options = new DistributedCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };
        
        try
        {
            await _distributedCache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
            return true;
        }
        catch (Exception ex)
        { 
            _logger.LogError(ex, RedisError);
            return false;
        }
        
    }

    public async Task<bool> RemoveData(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, RedisError);
            return false;
        }
    }
    
    public async Task<List<string>> GetRedisKeysOnPatternAsync(params string[] patterns)
    {
        try
        {
            IServer server = _redis.GetServer(_redis.GetEndPoints().First());
            
            List<string> result = await server
                .KeysAsync(pattern: RedisInstanceName + "*")
                .Select(key => key.ToString())
                .Where(key => patterns.All(key.Contains))
                .ToListAsync();

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, RedisError);
            return new List<string>();
        }
    }

    public async Task<bool> BulkDeleteAsync(List<string> keys)
    {
        try
        {
            RedisKey[] redisKeys = keys.Select(k => new RedisKey(k)).ToArray();

            await _redis.GetDatabase().KeyDeleteAsync(redisKeys);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, RedisError);
            return false;
        }
    }
}