using Microsoft.EntityFrameworkCore;
using TodoApp.Application.Dtos;
using TodoApp.Domain.Models;
using TodoApp.Infrastructure.Data;
using TodoApp.Infrastructure.Ropositories.IToDoRepo;


namespace TodoApp.Application.Ropositories
{
  public class ToDoRepo : IToDoRepo
    {

    private readonly AplicationDbContext _context;
    
    public ToDoRepo(AplicationDbContext context)
    {
      _context = context;
    }
    public async Task CreateAsync(ToDo toDo)
    {
      await _context.ToDos.AddAsync(toDo);
      await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ToDo toDo)
    {
      _context.ToDos.Remove(toDo);
      await _context.SaveChangesAsync();
    }

    public async Task<List<ToDo>> GetAllAsync(ToDoQuerySearch query)
    {
      return await _context.ToDos.ToListAsync();
    }

    public Task<List<ToDo>> GetAllAsync(int id)
    {
      throw new NotImplementedException();
    }

    public async Task<ToDo?> GetByIdAsync(int id)
    {
      return await _context.ToDos.FindAsync(id);
    }

    public async Task UpdateAsync(ToDo toDo)
    {
      _context.ToDos.Update(toDo);
      await _context.SaveChangesAsync();
    }
  }
}