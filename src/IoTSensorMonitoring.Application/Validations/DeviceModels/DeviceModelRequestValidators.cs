using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;
using IoTSensorMonitoring.Domain.Enums;

namespace IoTSensorMonitoring.Application.Validations.DeviceModels;

public class CreateDeviceModelRequestValidator : AbstractValidator<CreateDeviceModelRequest>
{
    public CreateDeviceModelRequestValidator()
    {
        RuleFor(x => x.Manufacturer)
            .NotEmpty().WithMessage("Manufacturer is required.")
            .MaximumLength(200).WithMessage("Manufacturer must be at most 200 characters.");

        RuleFor(x => x.ModelNumber)
            .NotEmpty().WithMessage("Model number is required.")
            .MaximumLength(100).WithMessage("Model number must be at most 100 characters.");

        RuleFor(x => x.SupportedMetrics)
            .NotEmpty().WithMessage("SupportedMetrics is required.")
            .MaximumLength(500).WithMessage("SupportedMetrics must be at most 500 characters.")
            .Must(ContainOnlyKnownMetrics)
            .WithMessage("SupportedMetrics must list known metrics (e.g. Temperature,Humidity,Pressure).");

        RuleFor(x => x.CalibrationPeriodDays)
            .GreaterThan(0).WithMessage("Calibration period must be greater than 0 days.")
            .When(x => x.CalibrationPeriodDays.HasValue);
    }

    internal static bool ContainOnlyKnownMetrics(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 && parts.All(part => Enum.TryParse<SensorMetric>(part, ignoreCase: true, out _));
    }
}

public class UpdateDeviceModelRequestValidator : AbstractValidator<UpdateDeviceModelRequest>
{
    public UpdateDeviceModelRequestValidator()
    {
        RuleFor(x => x.Manufacturer)
            .NotEmpty().WithMessage("Manufacturer is required.")
            .MaximumLength(200).WithMessage("Manufacturer must be at most 200 characters.");

        RuleFor(x => x.ModelNumber)
            .NotEmpty().WithMessage("Model number is required.")
            .MaximumLength(100).WithMessage("Model number must be at most 100 characters.");

        RuleFor(x => x.SupportedMetrics)
            .NotEmpty().WithMessage("SupportedMetrics is required.")
            .MaximumLength(500).WithMessage("SupportedMetrics must be at most 500 characters.")
            .Must(CreateDeviceModelRequestValidator.ContainOnlyKnownMetrics)
            .WithMessage("SupportedMetrics must list known metrics (e.g. Temperature,Humidity,Pressure).");

        RuleFor(x => x.CalibrationPeriodDays)
            .GreaterThan(0).WithMessage("Calibration period must be greater than 0 days.")
            .When(x => x.CalibrationPeriodDays.HasValue);
    }
}
