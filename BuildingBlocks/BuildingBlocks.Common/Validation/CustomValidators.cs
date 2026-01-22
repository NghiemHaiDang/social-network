using FluentValidation;

namespace BuildingBlocks.Common.Validation;

public static class CustomValidators
{
    /// <summary>
    /// Validates that the string is a valid email format
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");
    }

    /// <summary>
    /// Validates password strength
    /// </summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        int minLength = 8)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(minLength).WithMessage($"Password must be at least {minLength} characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }

    /// <summary>
    /// Validates username format
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidUsername<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        int minLength = 3,
        int maxLength = 50)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(minLength).WithMessage($"Username must be at least {minLength} characters.")
            .MaximumLength(maxLength).WithMessage($"Username must not exceed {maxLength} characters.")
            .Matches("^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores.");
    }

    /// <summary>
    /// Validates phone number format
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidPhoneNumber<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Matches(@"^\+?[\d\s\-\(\)]{10,}$")
            .WithMessage("Invalid phone number format.");
    }

    /// <summary>
    /// Validates that the string is a valid GUID
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidGuid<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Id is required.")
            .Must(x => Guid.TryParse(x, out _)).WithMessage("Invalid Id format.");
    }

    /// <summary>
    /// Validates that the value is not null or empty for nullable strings
    /// </summary>
    public static IRuleBuilderOptions<T, string?> NotNullOrWhiteSpace<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("This field is required.");
    }
}
