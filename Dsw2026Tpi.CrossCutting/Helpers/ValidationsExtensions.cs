using System.Text.RegularExpressions;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.CrossCutting.Helpers;

public static class ValidationsExtensions
{
    public const string EmailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$";
    public static bool IsEmailValid(this string? email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
            Regex.IsMatch(email, EmailPattern);
    }

    public static bool IsNameValid(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Length >= 3 && value.Length <= 100;
    }

    public static bool IsDescriptionValid(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Length >= 10 && value.Length <= 100;
    }

    public static bool IsLicenseNumberValid(this string? value)
    {
        if (value is null)
        return true;
        return !string.IsNullOrWhiteSpace(value) & value.Length <= 50;
    }

    public static bool IsDniValid (this long dni)
    {
        var dniString = dni.ToString();
        return dni > 0 && dniString.Length >= 7 && dniString.Length <= 8;
    }

    public static bool IsPasswordValid(this string? password)
    {
        return !string.IsNullOrWhiteSpace(password) && password.Length >= 8;
    }

    public static void ValidatePagination(int pageSize, int pageIndex)
    {
        if (pageSize < 1 || pageSize > 100)
            throw new ValidationException()
                .WithDetail(nameof(pageSize), "La cantidad de elementos por página debe estar entre 1 y 100.");

        if (pageIndex < 1)
            throw new ValidationException()
                .WithDetail(nameof(pageIndex), "El número de página debe ser mayor o igual a 1.");
    }
}
