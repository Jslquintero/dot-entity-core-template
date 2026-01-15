using AutoMapper;
using Template.Api.Models;
using Template.Model.Entities;

namespace Template.Api.Utils
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            // Add your AutoMapper mappings here
            // Example mappings:

            // User mappings
            CreateMap<User, UserViewModel>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles));

            CreateMap<LoginViewModel, User>();

            // Add more mappings as needed for your DTOs and entities
            // CreateMap<Entity, Dto>();
            // CreateMap<Dto, Entity>();
        }
    }
}