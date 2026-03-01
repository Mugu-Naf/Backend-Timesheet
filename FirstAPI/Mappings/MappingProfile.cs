using AutoMapper;
using FirstAPI.Models;
using FirstAPI.Models.DTOs;

namespace FirstAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Employee mappings
            CreateMap<Employee, EmployeeProfileDto>();
            CreateMap<EmployeeUpdateDto, Employee>();

            // Project mappings
            CreateMap<ProjectCreateDto, Project>();
            CreateMap<ProjectUpdateDto, Project>();
            CreateMap<Project, ProjectResponseDto>();

            // OvertimeRule mappings
            CreateMap<OvertimeRuleCreateDto, OvertimeRule>();
            CreateMap<OvertimeRuleUpdateDto, OvertimeRule>();
            CreateMap<OvertimeRule, OvertimeRuleResponseDto>();
        }
    }
}
