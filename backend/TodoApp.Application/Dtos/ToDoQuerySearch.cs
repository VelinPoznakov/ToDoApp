using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoApp.Application.Dtos
{
    public class ToDoQuerySearch
    {
        public string? Search { get; set; }

        public int Number { get; set; } = 1;

        public int PageSize { get; set; } = 5;


    }
}