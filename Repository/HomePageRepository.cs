using Contracts;
using Entities;
using Entities.Contexts;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class HomePageRepository: IHomePageRepository
    {
        private DbBecaContext _context;
        private BecaUser _currentUser;
        private Company _activeCompany;
        public HomePageRepository(IDependencies deps, IHttpContextAccessor httpContextAccessor)
        {
            _context = deps.context;
            _currentUser = (BecaUser)httpContextAccessor.HttpContext.Items["User"];
            _activeCompany = (Company)httpContextAccessor.HttpContext.Items["Company"];
        }

        public List<BecaHomePage> GetHomePageByUser()
        {
            return _context.BecaHomePages.Where(m => m.idProfile == _currentUser.idProfileDef(_activeCompany.idCompany)).ToList();
        }
    }
}
