using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using TodoApp.Services.Core;

using static TodoApp.GCommon.ApplicationConstants;

namespace TodoApp.Services.Tests.Redis;

// Unit tests for RedisCacheService with IDistributedCache and the Redis
// connection mocked. GetStringAsync / SetStringAsync are extension methods that
// route through GetAsync / SetAsync, so those are what Moq can intercept.
public class RedisCacheServiceTests
{
    private class SamplePayload
    {
        public int Count { get; set; }
        public string Label { get; set; } = null!;
    }

    private readonly Mock<IDistributedCache> _cache = new();
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IServer> _server = new();
    private readonly Mock<IDatabase> _database = new();

    public RedisCacheServiceTests()
    {
        EndPoint endPoint = new DnsEndPoint("localhost", 6379);

        _redis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new[] { endPoint });
        _redis.Setup(r => r.GetServer(endPoint, It.IsAny<object?>())).Returns(_server.Object);
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>())).Returns(_database.Object);
    }

    private RedisCacheService CreateService() => new RedisCacheService(
        _cache.Object,
        Mock.Of<ILogger<RedisCacheService>>(),
        _redis.Object);

    private void SetupKeysInRedis(params string[] keys)
        => _server.Setup(s => s.KeysAsync(
                It.IsAny<int>(),
                It.IsAny<RedisValue>(),
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CommandFlags>()))
            .Returns(ToAsyncKeys(keys));

    private static async IAsyncEnumerable<RedisKey> ToAsyncKeys(string[] keys)
    {
        foreach (string key in keys)
        {
            yield return new RedisKey(key);
        }

        await Task.CompletedTask;
    }

    // ===== GetData =====

    [Fact]
    public async Task GetData_WhenKeyMissing_ReturnsDefault()
    {
        _cache.Setup(c => c.GetAsync("missing", It.IsAny<CancellationToken>()))
              .ReturnsAsync((byte[]?)null);

        var service = CreateService();

        Assert.Null(await service.GetData<SamplePayload>("missing"));
    }

    [Fact]
    public async Task GetData_WhenStoredValueIsEmpty_ReturnsDefault()
    {
        _cache.Setup(c => c.GetAsync("empty", It.IsAny<CancellationToken>()))
              .ReturnsAsync(Array.Empty<byte>());

        var service = CreateService();

        Assert.Null(await service.GetData<SamplePayload>("empty"));
    }

    [Fact]
    public async Task GetData_DeserializesStoredJson()
    {
        SamplePayload stored = new SamplePayload { Count = 7, Label = "seven" };
        _cache.Setup(c => c.GetAsync("key", It.IsAny<CancellationToken>()))
              .ReturnsAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(stored)));

        var service = CreateService();
        SamplePayload? result = await service.GetData<SamplePayload>("key");

        Assert.NotNull(result);
        Assert.Equal(7, result!.Count);
        Assert.Equal("seven", result.Label);
    }

    [Fact]
    public async Task GetData_WhenStoredJsonIsInvalid_ReturnsDefaultInsteadOfThrowing()
    {
        // A bad entry (for example after a view model changed shape) must act
        // like a cache miss, not crash the page.
        _cache.Setup(c => c.GetAsync("bad", It.IsAny<CancellationToken>()))
              .ReturnsAsync(Encoding.UTF8.GetBytes("not json"));

        var service = CreateService();

        Assert.Null(await service.GetData<SamplePayload>("bad"));
    }

    [Fact]
    public async Task GetData_WhenRedisIsDown_ReturnsDefaultInsteadOfThrowing()
    {
        // The app must keep working from the database when Redis is unreachable.
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new TimeoutException("simulated Redis outage"));

        var service = CreateService();

        Assert.Null(await service.GetData<SamplePayload>("key"));
    }

    // ===== SetData =====

    [Fact]
    public async Task SetData_StoresSerializedJson_UnderTheGivenKey()
    {
        byte[]? captured = null;
        string? capturedKey = null;
        _cache.Setup(c => c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()))
              .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                  (k, v, _, _) => { capturedKey = k; captured = v; })
              .Returns(Task.CompletedTask);

        var service = CreateService();
        bool result = await service.SetData("count:all", new SamplePayload { Count = 50, Label = "todos" });

        Assert.True(result);
        Assert.Equal("count:all", capturedKey);
        SamplePayload? roundTrip = JsonSerializer.Deserialize<SamplePayload>(captured!);
        Assert.Equal(50, roundTrip!.Count);
        Assert.Equal("todos", roundTrip.Label);
    }

    [Fact]
    public async Task SetData_SetsAbsoluteAndSlidingExpiration()
    {
        DistributedCacheEntryOptions? capturedOptions = null;
        _cache.Setup(c => c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()))
              .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                  (_, _, o, _) => capturedOptions = o)
              .Returns(Task.CompletedTask);

        var service = CreateService();
        await service.SetData("key", new SamplePayload { Count = 1, Label = "x" });

        Assert.NotNull(capturedOptions);
        Assert.Equal(TimeSpan.FromMinutes(15), capturedOptions!.AbsoluteExpirationRelativeToNow);
        Assert.Equal(TimeSpan.FromMinutes(5), capturedOptions.SlidingExpiration);
    }

    [Fact]
    public async Task SetData_WhenRedisIsDown_ReturnsFalseInsteadOfThrowing()
    {
        _cache.Setup(c => c.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()))
              .ThrowsAsync(new TimeoutException("simulated Redis outage"));

        var service = CreateService();

        Assert.False(await service.SetData("key", new SamplePayload { Count = 1, Label = "x" }));
    }

    // ===== RemoveData =====

    [Fact]
    public async Task RemoveData_RemovesTheGivenKey()
    {
        _cache.Setup(c => c.RemoveAsync("key", It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        var service = CreateService();

        Assert.True(await service.RemoveData("key"));
        _cache.Verify(c => c.RemoveAsync("key", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveData_WhenRedisIsDown_ReturnsFalseInsteadOfThrowing()
    {
        _cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new TimeoutException("simulated Redis outage"));

        var service = CreateService();

        Assert.False(await service.RemoveData("key"));
    }

    // ===== GetRedisKeysOnPatternAsync =====

    [Fact]
    public async Task GetKeys_ScansOnlyThisAppsPrefix()
    {
        SetupKeysInRedis();

        var service = CreateService();
        await service.GetRedisKeysOnPatternAsync("anything");

        _server.Verify(s => s.KeysAsync(
            It.IsAny<int>(),
            It.Is<RedisValue>(p => p.ToString() == RedisInstanceName + "*"),
            It.IsAny<int>(),
            It.IsAny<long>(),
            It.IsAny<int>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetKeys_ReturnsOnlyKeysContainingEveryPattern()
    {
        SetupKeysInRedis(
            "TodoAppRedisCacheAll:todos:user1:group1:0",
            "TodoAppRedisCachePending:todos:user1:group1:0",
            "TodoAppRedisCacheAll:todos:user2:group1:0");

        var service = CreateService();
        List<string> keys = await service.GetRedisKeysOnPatternAsync("user1", "All");

        Assert.Equal("TodoAppRedisCacheAll:todos:user1:group1:0", Assert.Single(keys));
    }

    [Fact]
    public async Task GetKeys_NoPatterns_ReturnsEveryKeyOfTheApp()
    {
        // ClearRedisCache relies on this at startup.
        SetupKeysInRedis("TodoAppRedisCacheA", "TodoAppRedisCacheB");

        var service = CreateService();

        Assert.Equal(2, (await service.GetRedisKeysOnPatternAsync()).Count);
    }

    [Fact]
    public async Task GetKeys_WhenRedisIsDown_ReturnsAnEmptyList()
    {
        _redis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Throws(new TimeoutException("simulated Redis outage"));

        var service = CreateService();

        Assert.Empty(await service.GetRedisKeysOnPatternAsync("anything"));
    }

    // ===== BulkDeleteAsync =====

    [Fact]
    public async Task BulkDelete_DeletesTheExactKeyNames_WithoutAddingThePrefixAgain()
    {
        // Regression: the keys from KeysAsync already contain the instance name.
        // Deleting through IDistributedCache added it a second time, so nothing
        // was ever deleted and no error was raised.
        RedisKey[]? deleted = null;
        _database.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                 .Callback<RedisKey[], CommandFlags>((k, _) => deleted = k)
                 .ReturnsAsync(2L);

        var service = CreateService();
        bool result = await service.BulkDeleteAsync(new List<string>
        {
            "TodoAppRedisCacheAll:todos:u:g:0",
            "TodoAppRedisCachePending:todos:u:g:0"
        });

        Assert.True(result);
        Assert.Equal(
            new[] { "TodoAppRedisCacheAll:todos:u:g:0", "TodoAppRedisCachePending:todos:u:g:0" },
            deleted!.Select(k => k.ToString()));
        _cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkDelete_WhenRedisIsDown_ReturnsFalseInsteadOfThrowing()
    {
        _database.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                 .ThrowsAsync(new TimeoutException("simulated Redis outage"));

        var service = CreateService();

        Assert.False(await service.BulkDeleteAsync(new List<string> { "key" }));
    }
}
