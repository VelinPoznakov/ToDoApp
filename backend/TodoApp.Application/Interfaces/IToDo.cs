using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoApp.Application.Dtos;
using TodoApp.Domain.Models;

namespace TodoApp.Application.Ropositories
{
    public interface IToDo
    {
        Task<List<ToDo>> GetAllAsync(ToDoQuerySearch query);
        Task<ToDo?> GetByIdAsync(Guid id);
        Task<ToDo> CreateAsync(ToDo toDo);
        Task<ToDo> UpdateAsync(ToDo toDo);
        Task<bool> DeleteAsync(Guid id);


    }
}