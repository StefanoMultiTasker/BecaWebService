using AutoMapper;
using BecaWebService.Models.Users;
using Entities.Models;

namespace BecaWebService.Mappings
{
    public class MappingMenu : Profile
    {
        public MappingMenu()
        {
            CreateMap<UserMenu, UserMenuResponse>();
        }
    }
}
