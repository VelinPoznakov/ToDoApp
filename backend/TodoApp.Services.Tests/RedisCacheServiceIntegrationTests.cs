using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using TodoApp.Services.Core;

namespace TodoApp.Services.Tests;

// Integration test against the REAL Redis container (docker run redis, mapped
// to localhost:6379, per this project's dev setup). Unlike every other test
// in this suite, this one has an external dependency: if the container is not
// running, it fails with a connection error rather than being skipped. That
// failure is the intended signal ("go start Redis"), not a bug in the test.
public class RedisCacheServiceIntegrationTests
{
    private class SamplePayload
    {
        public int Count { get; set; }
        public string Label { get; set; } = null!;
    }

    private static RedisCacheService CreateRealService()
    {
        RedisCacheOptions options = new RedisCacheOptions
        {
            Configuration = "localhost:6379,abortConnect=false,connectTimeout=5000",
            InstanceName = "todoapp-tests:"
        };
        RedisCache cache = new RedisCache(Options.Create(options));
        return new RedisCacheService(cache);
    }

    [Fact]
    public async Task SetGetRemove_RoundTripsAgainstRealRedis()
    {
        RedisCacheService service = CreateRealService();
        string key = "integration-test:" + Guid.NewGuid();

        SamplePayload? before = await service.GetData<SamplePayload>(key);
        Assert.Null(before);

        await service.SetData(key, new SamplePayload { Count = 42, Label = "answer" });

        SamplePayload? stored = await service.GetData<SamplePayload>(key);
        Assert.NotNull(stored);
        Assert.Equal(42, stored!.Count);
        Assert.Equal("answer", stored.Label);

        await service.RemoveData(key);

        SamplePayload? afterRemove = await service.GetData<SamplePayload>(key);
        Assert.Null(afterRemove);
    }

    [Fact]
    public async Task Overwrite_ReplacesThePreviousValueUnderTheSameKey()
    {
        RedisCacheService service = CreateRealService();
        string key = "integration-test:" + Guid.NewGuid();

        await service.SetData(key, new SamplePayload { Count = 1, Label = "first" });
        await service.SetData(key, new SamplePayload { Count = 2, Label = "second" });

        SamplePayload? result = await service.GetData<SamplePayload>(key);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
        Assert.Equal("second", result.Label);

        await service.RemoveData(key);
    }
}
