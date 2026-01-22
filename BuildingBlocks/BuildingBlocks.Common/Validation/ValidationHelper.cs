using System.Text.RegularExpressions;

namespace BuildingBlocks.Common.Validation;

public static partial class ValidationHelper
{
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        return EmailRegex().IsMatch(email);
    }

    public static bool IsValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        return PhoneRegex().IsMatch(phoneNumber);
    }

    public static bool IsValidPassword(string? password, int minLength = 8)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < minLength)
            return false;

        bool hasUpperCase = password.Any(char.IsUpper);
        bool hasLowerCase = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
    }

    public static bool IsValidUsername(string? username, int minLength = 3, int maxLength = 50)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        if (username.Length < minLength || username.Length > maxLength)
            return false;

        return UsernameRegex().IsMatch(username);
    }

    public static bool IsNotNullOrEmpty(string? value) => !string.IsNullOrWhiteSpace(value);

    public static bool IsInRange(int value, int min, int max) => value >= min && value <= max;

    public static bool IsValidGuid(string? value)
    {
        return Guid.TryParse(value, out _);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\+?[\d\s\-\(\)]{10,}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9_]+$")]
    private static partial Regex UsernameRegex();
}
