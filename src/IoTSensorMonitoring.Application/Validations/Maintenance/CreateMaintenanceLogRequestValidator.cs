using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Validations.Maintenance;

public class CreateMaintenanceLogRequestValidator : AbstractValidator<CreateMaintenanceLogRequest>
{
    public CreateMaintenanceLogRequestValidator()
    {
        RuleFor(x => x.SensorId)
            .NotEmpty().WithMessage("SensorId is required.");

        RuleFor(x => x.ActionType)
            .IsInEnum().WithMessage("Invalid maintenance action type.");

        RuleFor(x => x.NextDueDate)
            .GreaterThan(x => x.PerformedAt!.Value)
            .WithMessage("Next due date must be after the performed date.")
            .When(x => x.NextDueDate.HasValue && x.PerformedAt.HasValue);
    }
}
