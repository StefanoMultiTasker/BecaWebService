using AutoMapper;
using BecaWebService.Models.Users;
using Entities.DataTransferObjects;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
