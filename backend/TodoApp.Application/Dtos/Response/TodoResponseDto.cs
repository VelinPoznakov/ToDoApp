using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TodoApp.Application.Dtos;
using TodoApp.Domain.Models;

namespace TodoApp.Application.Dtos
{
    public class TodoResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }
}

public class ColorResponseProfile : Profile
{
    public ColorResponseProfile()
    {
        CreateMap<ToDo, TodoResponseDto>();
    }
}