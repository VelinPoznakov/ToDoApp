namespace TodoApp.Services.Core.Contracts;

public interface IRedisCacheService
{
    Task<T?> GetData<T>(string key);
    Task<bool> SetData<T>(string key, T value);
    Task<bool> RemoveData(string key);
    Task<List<string>> GetRedisKeysOnPatternAsync(params string[] pattern);
    Task<bool> BulkDeleteAsync(List<string> keys);
}