using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TodoApp.Application.Dtos;
using TodoApp.Domain.Models;

namespace TodoApp.Application.Dtos
{
    public class CreateTodoDto
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        [Required, MaxLength(250)]
        public string Description { get; set; } = string.Empty;
    }
}

class CreateTdoDtoProfile : Profile
{
  public CreateTdoDtoProfile()
  {
    CreateMap<CreateTodoDto, ToDo>()
        .ForMember(dest => dest.Id, opt => opt.Ignore())
        .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
        .ForMember(dest => dest.ModifiedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

  }
}