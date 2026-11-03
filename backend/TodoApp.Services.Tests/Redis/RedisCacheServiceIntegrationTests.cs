using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using TodoApp.Services.Core;

using static TodoApp.GCommon.ApplicationConstants;

namespace TodoApp.Services.Tests.Redis;

// Integration tests against a REAL Redis, wired the same way as Program.cs (one
// shared connection, RedisInstanceName as the prefix). Mocks cannot prove that
// the key names returned by a scan are the ones a delete needs, this can.
//
// They are skipped unless TODOAPP_TEST_REDIS holds a connection string, see
// RedisFactAttribute. Every test uses a new user id in its keys and removes
// them again, so existing keys are never touched.
public class RedisCacheServiceIntegrationTests : IDisposable
{
    private class SamplePayload
    {
        public int Count { get; set; }
        public string Label { get; set; } = null!;
    }

    private readonly IConnectionMultiplexer _redis;
    private readonly RedisCache _cache;
    private readonly RedisCacheService _service;
    private readonly string _userId = Guid.NewGuid().ToString();

    public RedisCacheServiceIntegrationTests()
    {
        _redis = ConnectionMultiplexer.Connect(RedisFactAttribute.ConnectionString!);

        _cache = new RedisCache(Options.Create(new RedisCacheOptions
        {
            ConnectionMultiplexerFactory = () => Task.FromResult(_redis),
            InstanceName = RedisInstanceName
        }));

        _service = new RedisCacheService(
            _cache,
            Mock.Of<ILogger<RedisCacheService>>(),
            _redis);
    }

    public void Dispose()
    {
        List<string> leftovers = _service.GetRedisKeysOnPatternAsync(_userId).GetAwaiter().GetResult();
        _service.BulkDeleteAsync(leftovers).GetAwaiter().GetResult();

        _cache.Dispose();
        _redis.Dispose();
    }

    [RedisFact]
    public async Task SetGetRemove_RoundTrips()
    {
        string key = $"details:{_userId}";

        Assert.Null(await _service.GetData<SamplePayload>(key));

        Assert.True(await _service.SetData(key, new SamplePayload { Count = 42, Label = "answer" }));

        SamplePayload? stored = await _service.GetData<SamplePayload>(key);
        Assert.NotNull(stored);
        Assert.Equal(42, stored!.Count);
        Assert.Equal("answer", stored.Label);

        Assert.True(await _service.RemoveData(key));
        Assert.Null(await _service.GetData<SamplePayload>(key));
    }

    [RedisFact]
    public async Task SetData_SameKey_ReplacesThePreviousValue()
    {
        string key = $"details:{_userId}";

        await _service.SetData(key, new SamplePayload { Count = 1, Label = "first" });
        await _service.SetData(key, new SamplePayload { Count = 2, Label = "second" });

        SamplePayload? result = await _service.GetData<SamplePayload>(key);

        Assert.Equal(2, result!.Count);
        Assert.Equal("second", result.Label);
    }

    [RedisFact]
    public async Task GetKeys_ReturnsTheFullRedisNames_MatchingEveryPattern()
    {
        await _service.SetData($"All:todos:{_userId}:group:0", new SamplePayload());
        await _service.SetData($"Pending:todos:{_userId}:group:0", new SamplePayload());

        List<string> keys = await _service.GetRedisKeysOnPatternAsync(_userId, "All");

        Assert.Equal($"{RedisInstanceName}All:todos:{_userId}:group:0", Assert.Single(keys));
    }

    [RedisFact]
    public async Task BulkDelete_KeysFromGetKeys_ReallyRemovesThem()
    {
        // Regression: this was the double-prefix bug. The delete reported success
        // while every cached page stayed in Redis.
        string key = $"All:todos:{_userId}:group:0";
        await _service.SetData(key, new SamplePayload { Count = 1, Label = "x" });

        List<string> keys = await _service.GetRedisKeysOnPatternAsync(_userId);
        Assert.True(await _service.BulkDeleteAsync(keys));

        Assert.Null(await _service.GetData<SamplePayload>(key));
        Assert.Empty(await _service.GetRedisKeysOnPatternAsync(_userId));
    }

    [RedisFact]
    public async Task BulkDelete_LeavesOtherUsersKeysAlone()
    {
        string otherUserId = Guid.NewGuid().ToString();
        string otherKey = $"All:todos:{otherUserId}:group:0";

        await _service.SetData($"All:todos:{_userId}:group:0", new SamplePayload());
        await _service.SetData(otherKey, new SamplePayload { Count = 9, Label = "theirs" });

        await _service.BulkDeleteAsync(await _service.GetRedisKeysOnPatternAsync(_userId));

        Assert.NotNull(await _service.GetData<SamplePayload>(otherKey));

        await _service.RemoveData(otherKey);
    }
}
