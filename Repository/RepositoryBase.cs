using Contracts;
using Entities;
using Entities.Contexts;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Repository
{
    public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected DbBecaContext dbBecaContext { get; set; }
        private BecaUser _currentUser;
        private Company _currentCompany;

        //public RepositoryBase(IDependencies deps)
        //{
        //    this.dbBecaContext = deps.context;
        //}

        public RepositoryBase(IDependencies deps, IHttpContextAccessor httpContextAccessor)
        {
            this.dbBecaContext = deps.context;
            //urrentUser = deps.memoryContext.Users.Find(httpContextAccessor.HttpContext.Items["User"]);
            _currentUser = (BecaUser)httpContextAccessor.HttpContext!.Items["User"]!;
            _currentCompany = (Company)httpContextAccessor.HttpContext.Items["Company"]!;
        }

        public RepositoryBase(DbBecaContext repositoryContext, IHttpContextAccessor httpContextAccessor)
        {
            this.dbBecaContext = repositoryContext;
            _currentUser = (BecaUser)httpContextAccessor.HttpContext!.Items["User"]!;
            _currentCompany = (Company)httpContextAccessor.HttpContext.Items["Company"]!;
            //this.Settings();
        }

        public BecaUser CurrentUser() => _currentUser;
        public Company CurrentCompany() => _currentCompany;

        public IQueryable<T> GetAll()
        {
            return this.dbBecaContext.Set<T>().AsNoTracking();
        }

        public IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression)
        {
            return this.dbBecaContext.Set<T>().Where(expression).AsNoTracking();
        }

        public void Create(T entity)
        {
            this.dbBecaContext.Set<T>().Add(entity);
        }

        public void Update(T entity)
        {
            this.dbBecaContext.Set<T>().Update(entity);
        }

        public void Delete(T entity)
        {
            this.dbBecaContext.Set<T>().Remove(entity);
        }

    }
}
