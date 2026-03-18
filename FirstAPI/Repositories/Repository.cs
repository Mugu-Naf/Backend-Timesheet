using FirstAPI.Contexts;
using FirstAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FirstAPI.Repositories
{
    public class Repository<K, T> : IRepository<K, T> where T : class
    {
        private readonly TimeSheetContext _context;

        public Repository(TimeSheetContext context)
        {
            _context = context;
        }

        public async Task<T> Add(T entity)
        {
            _context.Set<T>().Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> Update(T entity)
        {
            _context.Set<T>().Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> Delete(K key)
        {
            var entity = await Get(key);
            if (entity == null)
                throw new Exception("Entity not found");
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> Get(K key)
        {
            var entity = await _context.Set<T>().FindAsync(key);
            if (entity == null)
                throw new Exception("Entity not found");
            return entity;
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public IQueryable<T> GetQueryable()
        {
            return _context.Set<T>();
        }
    }
}
