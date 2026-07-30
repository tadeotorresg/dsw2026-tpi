using System.Text.Json;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Data.Providers
{
    public class HolidayProvider : IHolidayProvider
    {
        private record HolidayCalendar(short Year, List<DateOnly> Holidays);
        private readonly HolidayCalendar _calendar;
        public HolidayProvider()
        {
            var path = Path.Combine(AppContext.BaseDirectory,"Sources","holidays.json");

            if (!File.Exists(path))
                throw new FileNotFoundException("No se encontró el archivo de feriados.", path);

            var json = File.ReadAllText(path);

            _calendar = JsonSerializer.Deserialize<HolidayCalendar>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
                ?? throw new InvalidOperationException("No se pudo cargar el calendario de feriados.");
        }

        public IReadOnlySet<DateOnly> GetHolidays(short year, byte month)
        {
            return _calendar.Holidays.Where(date =>date.Year == year && date.Month == month).ToHashSet();
        }
    }
}

