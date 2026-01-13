using TodoApp.Application.Dtos;
using TodoApp.Application.Services.Interfaces;
using TodoApp.Domain.Models;
using AutoMapper;
using TodoApp.Infrastructure.Ropositories.IToDoRepo;

namespace TodoApp.Application.Services
{
  public class ToDoService : IToDoService
    {
        private readonly IToDoRepo _repo;
        private readonly IMapper _mapper;

            
        public ToDoService(IToDoRepo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

    public async Task<TodoResponseDto> CreateAsync(CreateTodoDto dto)
    {
        var todo = _mapper.Map<ToDo>(dto);

        await _repo.CreateAsync(todo);

        todo.CreatedAt = DateTime.UtcNow;
        todo.ModifiedAt = DateTime.UtcNow;

        return _mapper.Map<TodoResponseDto>(todo);    
      
    }

    public async Task DeleteAsync(int id)
    {
        var todo = await _repo.GetByIdAsync(id)
                ?? throw new Exception("Todo not found");

        await _repo.DeleteAsync(todo);
    }

    public async Task<List<TodoResponseDto>> GetAllAsync()
    {
        var todos = await _repo.GetAllAsync();
        return _mapper.Map<List<TodoResponseDto>>(todos);
    }

    public async Task<TodoResponseDto?> GetByIdAsync(int id)
    {
            var todo = await _repo.GetByIdAsync(id);
            return todo == null ? null : _mapper.Map<TodoResponseDto>(todo);
    }

    public async Task<TodoResponseDto> UpdateAsync(int id, UpdateTodoDto dto)
    {
        var todo = _repo.GetByIdAsync(id).Result
                ?? throw new Exception("Todo not found");

        todo.ModifiedAt = DateTime.UtcNow;

        _mapper.Map(dto, todo);

        await _repo.UpdateAsync(todo);
        return _mapper.Map<TodoResponseDto>(todo);
        
    }
  }
}