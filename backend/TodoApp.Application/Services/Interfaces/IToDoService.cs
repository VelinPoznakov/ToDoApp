using TodoApp.Application.Dtos;
using TodoApp.Domain.Models;

namespace TodoApp.Application.Services.Interfaces
{
    public interface IToDoService
    {
        Task<List<TodoResponseDto>> GetAllAsync();
        Task<TodoResponseDto?> GetByIdAsync(int id);
        Task<TodoResponseDto> CreateAsync(CreateTodoDto dto);
        Task<TodoResponseDto> UpdateAsync(int id , UpdateTodoDto dto);
        Task DeleteAsync(int id);
    
    }
}