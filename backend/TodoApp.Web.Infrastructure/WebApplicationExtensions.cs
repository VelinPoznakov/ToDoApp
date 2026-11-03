
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using TodoApp.Data.Seeder.Contracts;
using TodoApp.Services.Core.Contracts;

namespace TodoApp.Web.Infrastructure
{
    public static class WebApplicationExtensions
    {
        public static IApplicationBuilder SeedRoles(this IApplicationBuilder applicationBuilder)
        {
            using IServiceScope scope = applicationBuilder
                .ApplicationServices
                .CreateScope();

            IIdentitySeeder roleSeeder = scope
                .ServiceProvider
                .GetRequiredService<IIdentitySeeder>();

            roleSeeder.RoleSeeder()
                .GetAwaiter()
                .GetResult();

            return applicationBuilder;
        }

        public static IApplicationBuilder SeedAdmin(this IApplicationBuilder applicationBuilder)
        {
            using IServiceScope scope = applicationBuilder
                .ApplicationServices
                .CreateScope();

            IIdentitySeeder adminSeeder = scope
                .ServiceProvider
                .GetRequiredService<IIdentitySeeder>();

            adminSeeder.AdminSeeder()
                .GetAwaiter()
                .GetResult();

            return applicationBuilder;
        }

        public static IApplicationBuilder ClearRedisCache(this IApplicationBuilder applicationBuilder)
        {
            using IServiceScope scope = applicationBuilder
                .ApplicationServices
                .CreateAsyncScope();
            
            IRedisCacheService redisCacheService = scope
                .ServiceProvider
                .GetRequiredService<IRedisCacheService>();

            List<string> keys = redisCacheService.GetRedisKeysOnPatternAsync()
                .GetAwaiter()
                .GetResult();
            
            redisCacheService.BulkDeleteAsync(keys)
                .GetAwaiter()
                .GetResult();

            return applicationBuilder;
        }
    }
}
