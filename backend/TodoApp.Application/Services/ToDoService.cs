using TodoApp.Application.Dtos;
using TodoApp.Application.Ropositories;
using TodoApp.Application.Services.Interfaces;
using TodoApp.Domain.Models;
using AutoMapper;
namespace TodoApp.Application.Services
{
  public class ToDoService : IToDoService
    {
        private readonly IToDo _repo;
        private readonly IMapper _mapper;

            
        public ToDoService(IToDo repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }
            
                    
        public async Task<ToDo> CreateAsync(CreateTodoDto dto)
        {
            // if (string.IsNullOrWhiteSpace(dto.Name))
            //     throw new ArgumentException("Name cannot be null or empty.", nameof(name));

            // var toDo = new ToDo
            // {
            //     Id = Guid.NewGuid(),
            //     Name = name.Trim(),
            //     Description = description?.Trim() ?? string.Empty
            // };

            var toDo = _mapper.Map<ToDo>(dto);

            return await _repo.CreateAsync(toDo);
        }


        public async Task<bool> DeleteAsync(Guid id) 
            => await _repo.DeleteAsync(id);
        
        // Retrieves all ToDo items
        public async Task<List<TodoResponseDto>> GetAllAsync(ToDoQuerySearch query)
        {
            var todos = await _repo.GetAllAsync(query);

            // convert domain model -> response dto
            return _mapper.Map<List<TodoResponseDto>>(todos);
        }
            

        // Retrieves a ToDo item by its unique identifier
        public  async Task<ToDo?> GetByIdAsync(Guid id) 
            => await _repo.GetByIdAsync(id);

        public async Task<ToDo?> UpdateAsync(Guid id, string name, string description)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing is null)
                return null;

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required.", nameof(name));

            // Modify the domain entity (service responsibility)
            existing.Name = name.Trim();
            existing.Description = description?.Trim() ?? string.Empty;

            // Persist (repo responsibility)
            return await _repo.UpdateAsync(existing); 
        }
  }
}