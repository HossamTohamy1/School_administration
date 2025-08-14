using Microsoft.EntityFrameworkCore;
using School_managment.Common.Models;
using School_managment.Infrastructure.Interface;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace School_managment.Infrastructure.Repositories
{
    public class GeneralRepository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly SchoolDbContext _context;

        public GeneralRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public IQueryable<T> GetAll()
        {
            return _context.Set<T>().Where(x => !x.IsDeleted);
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<T> GetByIdAsyncWithTracking(int id)
        {
            return await _context.Set<T>().AsTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public IQueryable<T> Get(Expression<Func<T, bool>> predicate)
        {
            return _context.Set<T>().Where(predicate).Where(x => !x.IsDeleted);
        }

        public async Task AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            _context.Set<T>().Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            entity.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdatePartialAsync(T entity, params string[] modifiedParams)
        {
            var existing = await _context.Set<T>().FindAsync(entity.Id);
            if (existing == null)
                return false;

            var entry = _context.Entry(existing);

            foreach (var prop in modifiedParams)
            {
                var newValue = entity.GetType().GetProperty(prop)?.GetValue(entity);
                entry.Property(prop).CurrentValue = newValue;
                entry.Property(prop).IsModified = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task RemoveClassSubjectsAsync(int classId, CancellationToken cancellationToken = default)
        {
            var oldRelations = await _context.ClassSubjects
                .Where(cs => cs.ClassId == classId)
                .ToListAsync(cancellationToken);

            _context.ClassSubjects.RemoveRange(oldRelations);
            await _context.SaveChangesAsync(cancellationToken);
        }
        public IQueryable<T> GetAllAsyncWithTracking(bool tracking = false)
        {
            var query = _context.Set<T>().Where(x => !x.IsDeleted);
            return tracking ? query : query.AsNoTracking();
        }




    }
}
