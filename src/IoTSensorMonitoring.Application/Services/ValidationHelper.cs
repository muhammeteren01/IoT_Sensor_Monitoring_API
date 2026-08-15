using FluentValidation;
using AppValidationException = IoTSensorMonitoring.Application.Common.Exceptions.ValidationException;

namespace IoTSensorMonitoring.Application.Services;

internal static class ValidationHelper
{
    public static async Task EnsureValidAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new AppValidationException(result.Errors);
        }
    }
}
