using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Validations.Sensors;

public class CreateSensorRequestValidator : AbstractValidator<CreateSensorRequest>
{
    public CreateSensorRequestValidator()
    {
        RuleFor(x => x.ZoneId)
            .NotEmpty().WithMessage("ZoneId is required.");

        RuleFor(x => x.DeviceModelId)
            .NotEmpty().WithMessage("DeviceModelId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters.");

        RuleFor(x => x.MacAddress)
            .NotEmpty().WithMessage("MAC address is required.")
            .MaximumLength(64).WithMessage("MAC address must be at most 64 characters.");

        RuleFor(x => x.FirmwareVersion)
            .MaximumLength(50).WithMessage("Firmware version must be at most 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.FirmwareVersion));
    }
}

public class UpdateSensorRequestValidator : AbstractValidator<UpdateSensorRequest>
{
    public UpdateSensorRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters.");

        RuleFor(x => x.FirmwareVersion)
            .MaximumLength(50).WithMessage("Firmware version must be at most 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.FirmwareVersion));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid sensor status.");
    }
}

public class SetSensorStatusRequestValidator : AbstractValidator<SetSensorStatusRequest>
{
    public SetSensorStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid sensor status.");
    }
}
