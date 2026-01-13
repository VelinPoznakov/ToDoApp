using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ToDoApp.Maui.Models.Request
{
    public class UpdateTodoDto
    {
        public string Name { get; set; }

        public string Description { get; set; }
    }
}