using Contracts;
using Entities;
using Entities.Contexts;
using Entities.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository
{
    public class BecaRepository : IBecaRepository
    {
        private DbBecaContext _context;
        private ILoggerManager _logger;

        public BecaRepository(IDependencies deps, ILoggerManager logger) //ILogger<GenericRepository> logger)
        {
            _context = deps.context;
            _logger = logger;
        }

        public List<Company> Companies(int? idCompany = null, string? name = null)
        {
            IQueryable<Company> query = _context.Companies;
            if (idCompany != null) query = query.Where(c => c.idCompany == idCompany);
            if (name != null) query = query.Where(c => c.CompanyName == name);

            return query.ToList();
        }

        public BecaViewAction BecaViewActions(string name) { 
            return _context.BecaViewActions.FirstOrDefault(a => a.ActionName == name);
        }
    }
}
