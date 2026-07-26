using System.Text.RegularExpressions;

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
        return !string.IsNullOrWhiteSpace(value);
    }
}



