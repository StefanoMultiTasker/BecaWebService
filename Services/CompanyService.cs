using BecaWebService.ExtensionsLib;
using Entities.Contexts;
using Entities.Models;
using Entities;

namespace BecaWebService.Services
{
    public interface ICompanyService
    {
        Company GetById(int id);
    }

    public class CompanyService : ICompanyService
    {
        private DbBecaContext _context;
        private IMyMemoryCache _memoryCache;
        //private DbMemoryContext _memoryContext;

        public CompanyService(IDependencies deps, DbBecaContext context)
        {
            _memoryCache = deps.memoryCache;
            _context = context;
        }

        public Company GetById(int id)
        {
            Company company = _memoryCache.GetOrSetCache<Company>($"CompanyById_{id}", () =>
            {
                return _context.Companies.Find(id);
            });

            //Company company = _memoryContext.Companies.Find(id);
            //if (company == null)
            //{
            //    company = _context.Companies.Find(id);
            //    _memoryContext.Companies.Add(company.deepCopy());
            //    _memoryContext.SaveChanges();
            //}
            if (company == null) throw new KeyNotFoundException("Company not found");
            return company;
        }
    }
}
