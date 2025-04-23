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
        private readonly DbBecaContext _context;
        private readonly IMyMemoryCache _memoryCache;

        public CompanyService(IMyMemoryCache memoryCache, DbBecaContext context)
        {
            _memoryCache = memoryCache;
            _context = context;
        }

        public Company GetById(int id)
        {
            return _memoryCache.GetOrSetCache<Company>($"CompanyById_{id}", () =>
            {
                var company = _context.Companies.Find(id);
                return company ?? throw new KeyNotFoundException($"Company with ID {id} not found");
            });
        }
    }
}
