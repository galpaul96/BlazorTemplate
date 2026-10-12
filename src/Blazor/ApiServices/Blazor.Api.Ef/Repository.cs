using Blazor.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Blazor.Api.Ef
{
    internal class Repository : IRepository
    {
        private readonly EfContext _context;

        public Repository(EfContext context)
        {
            _context = context;
        }

        public async Task<T> AddAsync<T>(T entity) where T : Audit
        {
            entity.CreatedDate = DateTime.UtcNow;
            var result = await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<T> GetByIdAsync<T>(Guid id, params Expression<Func<T, object>>[] predicates) where T : Audit
        {
            var result = _context.Set<T>().AsQueryable();

            foreach (var predicate in predicates)
            {
                result = result.Include(predicate);
            }

            return (await result.FirstOrDefaultAsync(x => x.Id == id))!;
        }

        public IQueryable<T> GetAllAsync<T>() where T : Audit
        {
            return _context.Set<T>().AsQueryable();

        }

        public async Task UpdateAsync<T>(T entity) where T : Audit
        {
            entity.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync<T>(Guid id) where T : Audit
        {
            var entityToDelete = await _context.Set<T>().FirstAsync(x => x.Id == id);
            entityToDelete.IsDeleted = true;
            entityToDelete.DeletedDate = DateTime.UtcNow;
            entityToDelete.UpdatedDate = entityToDelete.DeletedDate.Value;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync<T>(Guid id) where T : Audit
        {
            return await _context.Set<T>().AnyAsync(x => x.Id == id);
        }

        public async Task<bool> HealthCheck()
        {
            return await _context.Database.CanConnectAsync();
        }
    }
}
