namespace TodoApp.Services.Core.Contracts;

public interface IRedisCacheService
{
    Task<T?> GetData<T>(string key);
    Task SetData<T>(string key, T value);
    Task RemoveData(string key);
}