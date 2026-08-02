using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Data;

public class PersistenceEf: IPersistence
{
    private readonly Dsw2026TpiDbContext _context;

    public PersistenceEf(Dsw2026TpiDbContext context)
    {
        _context = context;
    }

    public async Task<T> Add<T>(T entity) where T : EntityBase
    {
        entity.CreatedAt = DateTime.Now;
        entity.UpdatedAt = DateTime.Now;
        await _context.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<T> Delete<T>(T entity) where T : EntityBase
    {
        var a = entity.Id;
        _context.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<T?> First<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).FirstOrDefaultAsync(predicate);
    }

    public async Task<IEnumerable<T>?> GetAll<T>(params string[] include) where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).ToListAsync();
    }

    public async Task<T?> GetById<T>(Guid id, params string[] include) where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<T>?> GetFiltered<T>(Expression<Func<T, bool>> predicate, params string[] include) where T : EntityBase
    {
        return await Include(_context.Set<T>(), include).Where(predicate).ToListAsync();
    }

    public async Task<T> Update<T>(T entity) where T : EntityBase
    {
        entity.UpdatedAt = DateTime.Now;
        _context.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task<Pagination<T>> Paginate<T, TKey>(int pageSize, int pageIndex, Expression<Func<T, bool>> predicate, Expression<Func<T, TKey>> sortOrder, params string[] includes) where T : EntityBase
    {
        pageSize = Math.Abs(pageSize);
        
        var requestedPage = Math.Abs(pageIndex) == 0 ? 1 : Math.Abs(pageIndex);
        var currentIndex = requestedPage - 1;

        var filtered = Include(_context.Set<T>(), includes)
            .Where(predicate)
            .OrderBy(sortOrder);

        var total = await filtered.CountAsync();
        
        async Task<Pagination<T>> GetPage(int index)
        {
            var data = await filtered.Skip(index * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

            return new Pagination<T>(pageSize, index + 1, data, total);
        }
        
        //la pagina existe
        if (total > pageSize * currentIndex)
        {
            return await GetPage(currentIndex);
        }

        //solo hay una pagina
        if (total < pageSize)
        {
            return new Pagination<T>(pageSize, 1, await filtered.ToListAsync(), total);
        }

        var targetPageIndex = currentIndex - 1;

        while (true)
        {
            if (total > targetPageIndex * pageSize)
            {
                return await GetPage(targetPageIndex);
            }

            targetPageIndex--;

            if (targetPageIndex < 0) return new Pagination<T>(pageSize, 1, [], 0);
        }
    }

    private static IQueryable<T> Include<T>(IQueryable<T> query, string[] includes) where T : EntityBase
    {
        var includedQuery = query;

        foreach (var include in includes)
        {
            includedQuery = includedQuery.Include(include);
        }
        return includedQuery;
    }
}
