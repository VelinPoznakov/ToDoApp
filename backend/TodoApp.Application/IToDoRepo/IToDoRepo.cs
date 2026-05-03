using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TodoApp.Domain.Models;

namespace TodoApp.Infrastructure.Ropositories.IToDoRepo
{
    public interface IToDoRepo
    {
        Task CreateAsync(ToDo toDo);
        Task DeleteAsync(ToDo toDo);
        Task<List<ToDo>> GetAllAsync();

        Task<ToDo?> GetByIdAsync(int id);
        Task UpdateAsync(ToDo toDo);
    }
}