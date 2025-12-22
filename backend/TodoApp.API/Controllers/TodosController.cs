using Microsoft.AspNetCore.Mvc;
using TodoApp.Application.Services;
using TodoApp.Application.Dtos;
using TodoApp.Application.Services.Interfaces;
using TodoApp.Domain.Models;
namespace TodoApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly IToDoService _service;

    public TodosController(IToDoService service)
    {
        _service = service;
    }

    // GET: api/todos
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ToDoQuerySearch query)
    {
        var result = await _service.GetAllAsync(query);
        return Ok(result);
    }

    // GET: api/todos/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Get a specific to-do item by ID
        var todo = await _service.GetByIdAsync(id);
        // If not found, return 404
        if (todo == null)
            return NotFound();

        // Return the to-do item
        return Ok(todo);
    }

    // POST: api/todos
    [HttpPost]
    public async Task<IActionResult> Create(CreateTodoDto dto)
    {
        // Create a new to-do item
        var todo = await _service.CreateAsync(dto);
        // Return the created to-do item
        return Ok(todo);
    }

    // PUT: api/todos/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateTodoDto dto)
    {
        // Update an existing to-do item
        var updated = await _service.UpdateAsync(id, dto.Name, dto.Description);
        // If not found, return 404
        if (updated == null)
            return NotFound();

        // Return the updated to-do item
        return Ok(updated);
    }

    // DELETE: api/todos/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        // Delete a to-do item
        var deleted = await _service.DeleteAsync(id);
        // If not found, return 404
        if (!deleted)
            return NotFound();

        // Return no content on successful deletion
        return NoContent();
    }
}
