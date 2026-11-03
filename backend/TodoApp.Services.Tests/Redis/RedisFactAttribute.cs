namespace TodoApp.Services.Tests.Redis;

// A [Fact] that only runs when a real Redis is available, so the normal test run
// has no external dependency. To run these tests, set the variable first:
//
//   $env:TODOAPP_TEST_REDIS = "localhost:6379,password=...,abortConnect=false"
public sealed class RedisFactAttribute : FactAttribute
{
    public const string ConnectionStringVariable = "TODOAPP_TEST_REDIS";

    public static string? ConnectionString
        => Environment.GetEnvironmentVariable(ConnectionStringVariable);

    public RedisFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            Skip = $"Needs a real Redis. Set {ConnectionStringVariable} to its connection string.";
        }
    }
}
