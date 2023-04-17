using Entities.Models;
using System.Linq.Expressions;

namespace Contracts
{
    public interface IRepositoryBase<T>
    {
        BecaUser CurrentUser();
        IQueryable<T> GetAll();
        IQueryable<T> GetByCondition(Expression<Func<T, bool>> expression);
        void Create(T entity);
        void Update(T entity);
        void Delete(T entity);
    }
}
