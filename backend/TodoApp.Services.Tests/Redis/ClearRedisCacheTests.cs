using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TodoApp.Services.Core.Contracts;
using TodoApp.Web.Infrastructure;

namespace TodoApp.Services.Tests.Redis;

public class ClearRedisCacheTests
{
    [Fact]
    public void ClearRedisCache_DeletesEveryKeyOfTheApp_AtStartup()
    {
        List<string> keys = new List<string> { "TodoAppRedisCacheA", "TodoAppRedisCacheB" };

        var redisCacheService = new Mock<IRedisCacheService>();
        redisCacheService
            .Setup(r => r.GetRedisKeysOnPatternAsync(It.Is<string[]>(p => p.Length == 0)))
            .ReturnsAsync(keys);

        var services = new ServiceCollection();
        services.AddScoped(_ => redisCacheService.Object);

        using ServiceProvider provider = services.BuildServiceProvider();
        IApplicationBuilder app = new ApplicationBuilder(provider);

        IApplicationBuilder result = app.ClearRedisCache();

        Assert.Same(app, result);
        redisCacheService.Verify(r => r.BulkDeleteAsync(keys), Times.Once);
    }
}
