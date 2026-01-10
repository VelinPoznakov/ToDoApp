using System.Net.Http.Json;
using System.Text.Json;
using ToDoApp.Maui.Models.Request;
using ToDoApp.Maui.Models.Response;
using ToDoApp.Maui.Services.Interfaces;

namespace ToDoApp.Maui.Services
{
    public class TodoApiService : ITodoApiService
    {
        private const string BaseUrl = "http://10.0.2.2:5166/api/todos";
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public TodoApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }
        
        
        public async Task<IReadOnlyList<TodoResponseDto>> GetAllTodosAsync(CancellationToken ct = default)
        {
            var response = await _httpClient.GetAsync(BaseUrl, ct);
            response.EnsureSuccessStatusCode();
            
            var todos = await response.Content.ReadFromJsonAsync<List<TodoResponseDto>>(_jsonSerializerOptions, ct);
            return todos ?? new List<TodoResponseDto>();
        }

        public async Task<TodoResponseDto> GetTodoByIdAsync(int id, CancellationToken ct = default)
        {
            var response = await _httpClient.GetAsync(BaseUrl, ct);
            response.EnsureSuccessStatusCode();
            
            var todo = await response.Content.ReadFromJsonAsync<TodoResponseDto>(
                _jsonSerializerOptions,
                ct);
            
            if (todo is null)
                throw new InvalidOperationException($"Todo with ID {id} not found.");
            
            return todo;
            
        }

        public async Task<TodoResponseDto> CreateTodoAsync(CreateTodoDto createTodoDto, CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsJsonAsync(BaseUrl,
                createTodoDto,
                _jsonSerializerOptions,
                ct);
            
            response.EnsureSuccessStatusCode();

            var createdTodo = await response.Content.ReadFromJsonAsync<TodoResponseDto>(
                _jsonSerializerOptions,
                ct);

            if (createdTodo is null)
                throw new InvalidOperationException("API did not return created todo.");

            return createdTodo;
        }

        public async Task UpdateTodoAsync(int id, UpdateTodoDto updateTodoDto, CancellationToken ct = default)
        {
            var url = $"{BaseUrl}/{id}";

            var response = await _httpClient.PutAsJsonAsync(
                url,
                updateTodoDto,
                _jsonSerializerOptions,
                ct);

            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteTodoAsync(int id, CancellationToken ct = default)
        {
            var url = $"{BaseUrl}/{id}";

            var response = await _httpClient.DeleteAsync(url, ct);
            response.EnsureSuccessStatusCode();
        }
    }
}