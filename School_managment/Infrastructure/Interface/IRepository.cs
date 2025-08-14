using School_managment.Common.Models;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace School_managment.Infrastructure.Interface
{
    public interface IRepository<T> where T : BaseEntity
    {
        IQueryable<T> GetAll();
        IQueryable<T> GetAllAsyncWithTracking(bool tracking = false);
        Task<T> GetByIdAsync(int id);

        Task<T> GetByIdAsyncWithTracking(int id);

        IQueryable<T> Get(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);

        Task UpdateAsync(T entity);

        Task DeleteAsync(T entity);

        Task<bool> UpdatePartialAsync(T entity, params string[] modifiedParams);
        Task RemoveClassSubjectsAsync(int classId, CancellationToken cancellationToken = default);

        Task SaveAsync();
    }
}
