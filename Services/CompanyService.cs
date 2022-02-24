using BecaWebService.ExtensionsLib;
using Entities.Contexts;
using Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BecaWebService.Services
{
    public interface ICompanyService
    {
        Company GetById(int id);
    }

    public class CompanyService : ICompanyService
    {
        private DbBecaContext _context;
        private DbMemoryContext _memoryContext;

        public CompanyService(DbBecaContext context, DbMemoryContext memoryContext)
        {
            _context = context;
            _memoryContext = memoryContext;
        }

        public Company GetById(int id)
        {
            Company company = _memoryContext.Companies.Find(id);
            if (company == null)
            {
                company = _context.Companies.Find(id);
                _memoryContext.Companies.Add(company.deepCopy());
                _memoryContext.SaveChanges();
            }
            if (company == null) throw new KeyNotFoundException("Company not found");
            return company;
        }
    }
}
