using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Dsw2026Tpi.Data;

public class PersistenceEf : IPersistence
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

    public async Task<int> Count<T>(Expression<Func<T, bool>> predicate) where T : EntityBase
    {
        return await _context.Set<T>().CountAsync(predicate);
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
        var filtered = Include(_context.Set<T>(), includes)
        .Where(predicate)
        .OrderBy(sortOrder)
        .ThenBy(entity => entity.Id);

        var total = await filtered.CountAsync();

        var offset = ((long)pageIndex - 1) * pageSize;

        if (offset >= total)
            return new Pagination<T>(pageSize, pageIndex, [], total);

        var data = await filtered
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync();

        return new Pagination<T>(pageSize, pageIndex, data, total);
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
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
    Func<Task<TResult>> action, Guid? doctorId = null)
    {
        if (doctorId.HasValue && _context.ChangeTracker.HasChanges())
            throw new InvalidOperationException("La transacción debe comenzar antes de modificar entidades.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        if (doctorId.HasValue)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"""
                SELECT [Id]
                FROM [Doctors] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {doctorId.Value}
                """);
            _context.ChangeTracker.Clear();
        }

        var result = await action();

        await transaction.CommitAsync();

        return result;
    }
}