using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.CrossCutting.Helpers
{
    public static class DayOfWeekConverter
    {
        public static string GetDayName(byte dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "LUNES",
                2 => "MARTES",
                3 => "MIERCOLES",
                4 => "JUEVES",
                5 => "VIERNES",
                6 => "SABADO",
                0 => "DOMINGO",
                _ => string.Empty
            };
        }

        public static DayOfWeek Parse(string? day)
        {
            if (string.IsNullOrWhiteSpace(day))
                throw new ValidationException()
                    .WithDetail(nameof(day), "Debe indicar un día válido.");

            return day.Trim().ToUpper() switch
            {
                "LUNES" => DayOfWeek.Monday,
                "MARTES" => DayOfWeek.Tuesday,
                "MIERCOLES" => DayOfWeek.Wednesday,
                "JUEVES" => DayOfWeek.Thursday,
                "VIERNES" => DayOfWeek.Friday,
                "SABADO" => DayOfWeek.Saturday,
                "DOMINGO" => DayOfWeek.Sunday,
                _ => throw new ValidationException()
                        .WithDetail(nameof(day), "El día no es válido.")
            };
        }
    }
}
