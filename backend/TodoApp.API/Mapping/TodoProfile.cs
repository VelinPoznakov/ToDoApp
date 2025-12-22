using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TodoApp.Domain.Models;
using TodoApp.Application.Dtos;

namespace TodoApp.API.Mapping;

public class TodoProfile : Profile
{
    public TodoProfile()
    {
        CreateMap<ToDo, TodoResponseDto>();
        CreateMap<CreateTodoDto, ToDo>()
            .ForMember(x => x.Id, opt => opt.MapFrom(_ => Guid.NewGuid()));
    }
}
