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
        protected DbBecaContext dbdatiContext { get; set; }
        private BecaUser _currentUser;

        public RepositoryBase(IDependencies deps)
        {
            this.dbdatiContext = deps.context;
        }

        public RepositoryBase(IDependencies deps, HttpContext httpContext)
        {
            this.dbdatiContext = deps.context;
            _currentUser = deps.memoryContext.Users.Find(httpContext.Items["User"]);
        }

        public RepositoryBase(DbBecaContext repositoryContext, IHttpContextAccessor httpContextAccessor)
        {
            this.dbdatiContext = repositoryContext;
            //this.Settings();
        }

        public BecaUser CurrentUser() => _currentUser;

        public IQueryable<T> GetAll()
        {
            return this.dbdatiContext.Set<T>().AsNoTracking();
        }

        public IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression)
        {
            return this.dbdatiContext.Set<T>().Where(expression).AsNoTracking();
        }

        public void Create(T entity)
        {
            this.dbdatiContext.Set<T>().Add(entity);
        }

        public void Update(T entity)
        {
            this.dbdatiContext.Set<T>().Update(entity);
        }

        public void Delete(T entity)
        {
            this.dbdatiContext.Set<T>().Remove(entity);
        }

    }
}
