using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Dtos;
using TodoApp.Domain.Models;
using TodoApp.Infrastructure.Data;


namespace TodoApp.Application.Ropositories
{
  public class ToDoRepo : IToDo
    {

    private readonly AplicationDbContext _context;
    
    public ToDoRepo(AplicationDbContext context)
    {
      _context = context;
    }
    public async Task<ToDo> CreateAsync(ToDo toDo)
    {
      // marking the entity as added
      await _context.ToDos.AddAsync(toDo);

      // saving the changes to the database
      await _context.SaveChangesAsync();
      
      // returning the created entity
      return toDo;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
      // finding the entity by id
      var toDoModel = await _context.ToDos.FindAsync(id);

      // if not found, return false
      if (toDoModel == null)
      {
        return false;
      }
      // removing the entity
      _context.ToDos.Remove(toDoModel);
      // saving the changes to the database
      await _context.SaveChangesAsync();
      // returning true to indicate successful deletion
      return true;
    }

    public async Task<List<ToDo>> GetAllAsync(ToDoQuerySearch query)
    {
      var todosQuery = _context.ToDos.AsNoTracking();

      if (!string.IsNullOrWhiteSpace(query.Search))
      {
        string search = query.Search.Trim().ToLower();

        todosQuery = todosQuery.Where(t =>
        t.Name.ToLower().Contains(search));
      }
      
      var skipNumber = (query.Number - 1) * query.PageSize;
      
      return await todosQuery.Skip(skipNumber).Take(query.PageSize).ToListAsync();
    }

    public async Task<ToDo?> GetByIdAsync(Guid id)
    {
      // finding the entity by id
      return await _context.ToDos.FindAsync(id);
    }

    public async Task<ToDo> UpdateAsync(ToDo toDo)
    {
      // marking the entity as modified
      _context.ToDos.Update(toDo);
      // saving the changes to the database
      await _context.SaveChangesAsync();
      // returning the updated entity
      return toDo;
    }
  }
}