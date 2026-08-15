using FluentValidation;
using IoTSensorMonitoring.Application.DTOs;

namespace IoTSensorMonitoring.Application.Validations.Companies;

public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters.");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("A valid email is required.")
            .MaximumLength(256).WithMessage("Contact email must be at most 256 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
    }
}

public class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must be at most 200 characters.");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("A valid email is required.")
            .MaximumLength(256).WithMessage("Contact email must be at most 256 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
    }
}
