using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using TodoApp.Services.Core;

namespace TodoApp.Services.Tests;

// Unit tests for RedisCacheService with IDistributedCache mocked. GetStringAsync
// / SetStringAsync are extension methods that route through GetAsync / SetAsync
// / RemoveAsync, so those are the members Moq can actually intercept.
public class RedisCacheServiceTests
{
    private class SamplePayload
    {
        public int Count { get; set; }
        public string Label { get; set; } = null!;
    }

    private readonly Mock<IDistributedCache> _cache = new();

    private RedisCacheService CreateService() => new RedisCacheService(_cache.Object);

    // ===== GetData =====

    [Fact]
    public async Task GetData_WhenKeyMissing_ReturnsDefault()
    {
        _cache.Setup(c => c.GetAsync("missing", It.IsAny<CancellationToken>()))
              .ReturnsAsync((byte[]?)null);

        var service = CreateService();
        SamplePayload? result = await service.GetData<SamplePayload>("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetData_WhenStoredValueIsEmptyByteArray_ReturnsDefault()
    {
        // Encoding.UTF8.GetString(Array.Empty<byte>()) == "" -> IsNullOrEmpty
        // short-circuits before JsonSerializer ever sees it.
        _cache.Setup(c => c.GetAsync("empty", It.IsAny<CancellationToken>()))
              .ReturnsAsync(Array.Empty<byte>());

        var service = CreateService();
        SamplePayload? result = await service.GetData<SamplePayload>("empty");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetData_DeserializesStoredJson()
    {
        SamplePayload stored = new SamplePayload { Count = 7, Label = "seven" };
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(stored));
        _cache.Setup(c => c.GetAsync("key", It.IsAny<CancellationToken>()))
              .ReturnsAsync(bytes);

        var service = CreateService();
        SamplePayload? result = await service.GetData<SamplePayload>("key");

        Assert.NotNull(result);
        Assert.Equal(7, result!.Count);
        Assert.Equal("seven", result.Label);
    }

    [Fact]
    public async Task GetData_WhenStoredJsonDoesNotMatchT_ThrowsJsonException()
    {
        // Characterizes current behaviour: a malformed/incompatible cached
        // entry (e.g. after a DTO shape change) is not treated as a cache
        // miss - it throws instead of falling back to the caller re-fetching.
        byte[] bytes = Encoding.UTF8.GetBytes("not json");
        _cache.Setup(c => c.GetAsync("bad", It.IsAny<CancellationToken>()))
              .ReturnsAsync(bytes);

        var service = CreateService();

        await Assert.ThrowsAsync<JsonException>(
            () => service.GetData<SamplePayload>("bad"));
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
        await service.SetData("count:all", new SamplePayload { Count = 50, Label = "todos" });

        Assert.Equal("count:all", capturedKey);
        Assert.NotNull(captured);
        SamplePayload? roundTrip = JsonSerializer.Deserialize<SamplePayload>(captured!);
        Assert.Equal(50, roundTrip!.Count);
        Assert.Equal("todos", roundTrip.Label);
    }

    [Fact]
    public async Task SetData_SetsAbsoluteAndSlidingExpiration()
    {
        // Pins the hardcoded policy currently baked into SetData: every key,
        // regardless of what it holds, gets the same 15 min / 5 min lifetime.
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

    // ===== RemoveData =====

    [Fact]
    public async Task RemoveData_RemovesTheGivenKey()
    {
        _cache.Setup(c => c.RemoveAsync("key", It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        var service = CreateService();
        await service.RemoveData("key");

        _cache.Verify(c => c.RemoveAsync("key", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== Failure handling =====

    [Fact]
    public async Task GetData_WhenCacheThrows_PropagatesTheException()
    {
        // Characterizes current behaviour: RedisCacheService has no try/catch
        // around the underlying cache call. If Redis is unreachable, this
        // throws all the way up to the caller instead of degrading to "miss".
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new TimeoutException("simulated Redis outage"));

        var service = CreateService();

        await Assert.ThrowsAsync<TimeoutException>(
            () => service.GetData<SamplePayload>("key"));
    }
}
