namespace TodoApp.Data.Repositories;

public class BaseRepository
{
    private readonly TodoDbContext _dbContext;

    protected BaseRepository(TodoDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected TodoDbContext DbContext => _dbContext;

    protected async Task<int> SaveChangesAsync()
    {
        return await DbContext.SaveChangesAsync();
    }
}