namespace Dsw2026Tpi.Domain.Entities;

public record Pagination<T>(int PageSize, int PageIndex, IEnumerable<T> Data, int Total)
{
    public Pagination<TMap> Map<TMap>(Func<T, TMap> map) => new(PageSize, PageIndex, Data.Select(map), Total);

    public static Pagination<T> Empty => new(0, 0, [], 0);
};
