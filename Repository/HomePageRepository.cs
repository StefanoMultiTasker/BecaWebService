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
    public class HomePageRepository : IHomePageRepository
    {
        private DbBecaContext _context;
        private BecaUser _currentUser;
        private Company _activeCompany;
        public HomePageRepository(DbBecaContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _currentUser = (BecaUser)httpContextAccessor.HttpContext!.Items["User"]!;
            _activeCompany = (Company)httpContextAccessor.HttpContext.Items["Company"]!;
        }

        public List<BecaHomePage> GetHomePageByUser()
        {
            return _context.BecaHomePages.Where(m => m.idProfile == _currentUser.idProfileDef(_activeCompany.idCompany)).ToList();
        }

        public List<BecaHomeBuild> GetHomeBuildByUser(int[] idProfiles)
        {
            idProfiles = [_currentUser.idProfileDef(_activeCompany.idCompany).Value];
            return [.. _context.BecaHomeBuild
                .Where(m => idProfiles.Contains(m.idProfile))
                .GroupBy(m => m.idHomeBrick)
                .Select(g => g.First())];
        }

        public BecaHomeBuild? GetHomeBrick(int idHomeBrick, int[] idProfiles)
        {
            return _context.BecaHomeBuild.FirstOrDefault(m => idProfiles.Contains(m.idProfile) && m.idHomeBrick == idHomeBrick);
        }
    }
}
