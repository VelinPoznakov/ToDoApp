using TodoApp.Application.Dtos;
using TodoApp.Domain.Models;

namespace TodoApp.Application.Services.Interfaces
{
    public interface IToDoService
    {
        Task<List<TodoResponseDto>> GetAllAsync(ToDoQuerySearch query);
        Task<ToDo?> GetByIdAsync(Guid id);
        Task<ToDo> CreateAsync(CreateTodoDto dto);
        Task<ToDo?> UpdateAsync(Guid id, string name, string description);
        Task<bool> DeleteAsync(Guid id);
    
    }
}