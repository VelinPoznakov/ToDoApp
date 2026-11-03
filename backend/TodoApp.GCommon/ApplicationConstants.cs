namespace TodoApp.GCommon;

public static class ApplicationConstants
{
    public const string DateFormat = "dd-MM-yyyy";
    public const int TodosDefaultPageSize = 10;
    public const string RedisInstanceName = "TodoAppRedisCache";
    
    public enum TodosSearchParameter
    {
        All = 0,
        Completed = 1,
        Pending = 2
    }
}