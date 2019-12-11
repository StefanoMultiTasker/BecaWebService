using Contracts;
using Entities.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Repository
{
    public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected DbdatiContext dbdatiContext { get; set; }

        public RepositoryBase(DbdatiContext repositoryContext)
        {
            this.dbdatiContext = repositoryContext;
        }

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
