using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Validations.Facilities;

public class CreateFacilityRequestValidator : AbstractValidator<CreateFacilityRequest>
{
    public CreateFacilityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must be at most 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.FloorCount)
            .InclusiveBetween(1, 200).WithMessage("Floor count must be between 1 and 200.");
    }
}

public class UpdateFacilityRequestValidator : AbstractValidator<UpdateFacilityRequest>
{
    public UpdateFacilityRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters.");

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must be at most 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.FloorCount)
            .InclusiveBetween(1, 200).WithMessage("Floor count must be between 1 and 200.");
    }
}
