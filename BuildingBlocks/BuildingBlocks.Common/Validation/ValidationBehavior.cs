using BuildingBlocks.Common.Exceptions;
using FluentValidation;

namespace BuildingBlocks.Common.Validation;

/// <summary>
/// Service to validate objects using FluentValidation
/// </summary>
public interface IValidationService
{
    Task ValidateAndThrowAsync<T>(T instance, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default);
}

public class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ValidateAndThrowAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var validator = _serviceProvider.GetService(typeof(IValidator<T>)) as IValidator<T>;

        if (validator == null)
            return;

        var result = await validator.ValidateAsync(instance, cancellationToken);

        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray());

            throw new BadRequestException(errors);
        }
    }

    public async Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken cancellationToken = default)
    {
        var validator = _serviceProvider.GetService(typeof(IValidator<T>)) as IValidator<T>;
        var validationResult = new ValidationResult();

        if (validator == null)
            return validationResult;

        var result = await validator.ValidateAsync(instance, cancellationToken);

        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
            {
                validationResult.AddError(error.PropertyName, error.ErrorMessage);
            }
        }

        return validationResult;
    }
}
