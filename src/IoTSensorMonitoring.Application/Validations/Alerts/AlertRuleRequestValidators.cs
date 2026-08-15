using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Validations.Alerts;

public class CreateAlertRuleRequestValidator : AbstractValidator<CreateAlertRuleRequest>
{
    public CreateAlertRuleRequestValidator()
    {
        RuleFor(x => x.SensorId)
            .NotEmpty().WithMessage("SensorId is required.");

        RuleFor(x => x.Metric)
            .IsInEnum().WithMessage("Invalid sensor metric.");

        RuleFor(x => x.Operator)
            .IsInEnum().WithMessage("Invalid comparison operator.");

        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Invalid alert severity.");
    }
}

public class UpdateAlertRuleRequestValidator : AbstractValidator<UpdateAlertRuleRequest>
{
    public UpdateAlertRuleRequestValidator()
    {
        RuleFor(x => x.Metric)
            .IsInEnum().WithMessage("Invalid sensor metric.");

        RuleFor(x => x.Operator)
            .IsInEnum().WithMessage("Invalid comparison operator.");

        RuleFor(x => x.Severity)
            .IsInEnum().WithMessage("Invalid alert severity.");
    }
}
