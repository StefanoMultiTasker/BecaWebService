using Contracts;
using Entities.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Entities.Models;
using Entities;

namespace Repository
{
    public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected DbBecaContext dbBecaContext { get; set; }
        private BecaUser _currentUser;
        private Company _currentCompany;

        public RepositoryBase(IDependencies deps)
        {
            this.dbBecaContext = deps.context;
        }

        public RepositoryBase(IDependencies deps, HttpContext httpContext)
        {
            this.dbBecaContext = deps.context;
            _currentUser = deps.memoryContext.Users.Find(httpContext.Items["User"]);
        }

        public RepositoryBase(DbBecaContext repositoryContext, IHttpContextAccessor httpContextAccessor)
        {
            this.dbBecaContext = repositoryContext;
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
