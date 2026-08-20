using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace TodoApp.Services.Tests;

// These tests mirror Program.cs's DI registration order for IDistributedCache
// to prove which implementation actually gets resolved at runtime.
//
// Program.cs calls AddStackExchangeRedisCache(...) first, then later calls
// AddDistributedMemoryCache() (originally added for session storage, before
// Redis existed in this project). Both calls "succeed" with no error either
// way, so this can only be answered by actually building the container and
// resolving the service - guessing from the call order alone is not reliable
// without checking each method's exact registration semantics (Add vs TryAdd).
public class RedisRegistrationTests
{
    [Fact]
    public void RegistrationOrder_MatchingProgramCs_ResolvesRedisCache()
    {
        var services = new ServiceCollection();

        // 1) Program.cs line ~39: AddStackExchangeRedisCache runs first.
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379,abortConnect=false,connectTimeout=5000";
            options.InstanceName = "TodoAppRedisCache";
        });

        // 2) Program.cs line ~79: AddDistributedMemoryCache runs after it.
        services.AddDistributedMemoryCache();

        using ServiceProvider provider = services.BuildServiceProvider();
        IDistributedCache cache = provider.GetRequiredService<IDistributedCache>();

        // If this were MemoryDistributedCache, Redis would be registered but
        // never actually used at runtime - every IDistributedCache injection
        // in the app would resolve to the in-process memory cache instead.
        // The concrete type is internal (RedisCacheImpl in this package
        // version), so assert by namespace/assembly rather than a fixed name.
        Assert.NotEqual("MemoryDistributedCache", cache.GetType().Name);
        Assert.Contains("StackExchangeRedis", cache.GetType().Namespace);
    }

    [Fact]
    public void ReversedOrder_MemoryCacheFirst_StillResolvesRedisCache()
    {
        // Sanity check: confirms whether order matters at all for these two
        // extension methods, by registering them the opposite way round.
        var services = new ServiceCollection();

        services.AddDistributedMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379,abortConnect=false,connectTimeout=5000";
            options.InstanceName = "TodoAppRedisCache";
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        IDistributedCache cache = provider.GetRequiredService<IDistributedCache>();

        Assert.NotEqual("MemoryDistributedCache", cache.GetType().Name);
        Assert.Contains("StackExchangeRedis", cache.GetType().Namespace);
    }
}
