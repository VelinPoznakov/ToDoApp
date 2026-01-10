using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToDoApp.Maui.Models.Request;
using ToDoApp.Maui.Models.Response;

namespace ToDoApp.Maui.Services.Interfaces
{
    public interface ITodoApiService
    {
        Task<IReadOnlyList<TodoResponseDto>> GetAllTodosAsync(CancellationToken ct = default);
        Task<TodoResponseDto> GetTodoByIdAsync(int id, CancellationToken ct = default);
        Task<TodoResponseDto> CreateTodoAsync(CreateTodoDto createTodoDto, CancellationToken ct = default);
        Task UpdateTodoAsync(int id, UpdateTodoDto updateTodoDto, CancellationToken ct = default);
        Task DeleteTodoAsync(int id, CancellationToken ct = default);
    }
}