using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Validations.Zones;

public class CreateZoneRequestValidator : AbstractValidator<CreateZoneRequest>
{
    public CreateZoneRequestValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty().WithMessage("FacilityId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters.");

        RuleFor(x => x.FloorLevel)
            .InclusiveBetween(-20, 200).WithMessage("Floor level must be between -20 and 200.");
    }
}

public class UpdateZoneRequestValidator : AbstractValidator<UpdateZoneRequest>
{
    public UpdateZoneRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters.");

        RuleFor(x => x.FloorLevel)
            .InclusiveBetween(-20, 200).WithMessage("Floor level must be between -20 and 200.");
    }
}
