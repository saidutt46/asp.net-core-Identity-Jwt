using System;
using Application.Data.Models;
using Application.Dto.Response;
using AutoMapper;

namespace Application.Dto.Helpers
{
    public class DataProfile : Profile
    {
        public DataProfile()
        {
            CreateMap<ApplicationUser, UserProfileDto>();
            CreateMap<ApplicationUser, UserProfileDtoWithRoles>();
        }
    }
}
