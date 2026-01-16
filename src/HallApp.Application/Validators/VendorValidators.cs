using FluentValidation;
using HallApp.Application.DTOs.Vendors;

namespace HallApp.Application.Validators;

public class CreateVendorDtoValidator : AbstractValidator<CreateVendorDto>
{
    public CreateVendorDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Vendor name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required");

        RuleFor(x => x.VendorTypeId)
            .GreaterThan(0).WithMessage("Vendor type is required");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Website)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("Invalid website URL");
    }
}

public class UpdateVendorDtoValidator : AbstractValidator<UpdateVendorDto>
{
    public UpdateVendorDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Vendor name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.VendorTypeId)
            .GreaterThan(0).WithMessage("Vendor type is required");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.Website)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrEmpty(x.Website))
            .WithMessage("Invalid website URL");

        RuleFor(x => x.CommercialRegistrationNumber)
            .MaximumLength(50).WithMessage("Commercial registration cannot exceed 50 characters");

        RuleFor(x => x.VatNumber)
            .MaximumLength(50).WithMessage("VAT number cannot exceed 50 characters");
    }
}
