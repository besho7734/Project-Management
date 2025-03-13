using AutoMapper;
using Project_Management.Models;
using Project_Management.Models.DTO;

namespace Project_Management
{
    public class MappingConfig : Profile
    {
        public MappingConfig()
        {
            CreateMap<Project , ProjectDTO>().ReverseMap();

            CreateMap<Project , ProjectCreateDTO>().ReverseMap();

            CreateMap<task ,TaskDTO>().ReverseMap();

            CreateMap<ApplicationUser, UserDTO>().ReverseMap();

            CreateMap<ApplicationUser, GetUserToReturnDTO>().ReverseMap();
        }
    }
}
