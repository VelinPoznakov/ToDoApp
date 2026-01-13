using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TodoApp.Application.Dtos;
using TodoApp.Domain.Models;

namespace TodoApp.Application.Dtos
{
    public class UpdateTodoDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime ForDate { get; set; }
    }
}

class UpdateTodoDtoProfile : Profile
{
  public UpdateTodoDtoProfile()
  {
        CreateMap<UpdateTodoDto, ToDo>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsCompleted, opt => opt.Ignore());

    }
}