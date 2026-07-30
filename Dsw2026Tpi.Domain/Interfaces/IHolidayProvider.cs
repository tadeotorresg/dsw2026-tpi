
namespace Dsw2026Tpi.Domain.Interfaces
{
    public interface IHolidayProvider
    {
        IReadOnlySet<DateOnly> GetHolidays(short year, byte month);
    }
}
