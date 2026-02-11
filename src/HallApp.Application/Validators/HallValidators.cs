using FluentValidation;
using HallApp.Application.DTOs.Halls.Hall;
using HallApp.Application.DTOs.Booking;

namespace HallApp.Application.Validators;

public class HallCreateDtoValidator : AbstractValidator<HallCreateDto>
{
    public HallCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Hall name is required.")
            .MaximumLength(100).WithMessage("Hall name cannot exceed 100 characters.")
            .MinimumLength(2).WithMessage("Hall name must be at least 2 characters.");

        RuleFor(x => x.CommercialRegistration)
            .InclusiveBetween(100000000, 9999999999)
            .WithMessage("Commercial Registration must be 9 or 10 digits.");

        RuleFor(x => x.Vat)
            .GreaterThanOrEqualTo(0)
            .WithMessage("VAT number must be a positive number.")
            .LessThanOrEqualTo(999999999999999)
            .WithMessage("VAT number must not exceed 15 digits.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\+?[0-9]{9,15}$").WithMessage("Phone number must be 9-15 digits, optionally starting with +.");

        RuleFor(x => x.WhatsApp)
            .Matches(@"^\+?[0-9]{9,15}$").WithMessage("WhatsApp number must be 9-15 digits, optionally starting with +.")
            .When(x => !string.IsNullOrEmpty(x.WhatsApp));

        // Gender validation
        RuleFor(x => x.Gender)
            .InclusiveBetween(1, 3).WithMessage("Gender must be 1 (Male), 2 (Female), or 3 (Both).");

        // Male capacity and pricing validation
        RuleFor(x => x.MaleMin)
            .GreaterThanOrEqualTo(0).WithMessage("Male minimum capacity must be 0 or greater.")
            .LessThanOrEqualTo(x => x.MaleMax)
            .When(x => x.MaleActive)
            .WithMessage("Male minimum capacity must be less than or equal to maximum.");

        RuleFor(x => x.MaleMax)
            .GreaterThan(0)
            .When(x => x.MaleActive)
            .WithMessage("Male maximum capacity must be greater than 0 when male section is active.");

        RuleFor(x => x.MaleWeekDays)
            .GreaterThanOrEqualTo(0).WithMessage("Male weekday price must be 0 or greater.");

        RuleFor(x => x.MaleWeekEnds)
            .GreaterThanOrEqualTo(0).WithMessage("Male weekend price must be 0 or greater.");

        // Female capacity and pricing validation
        RuleFor(x => x.FemaleMin)
            .GreaterThanOrEqualTo(0).WithMessage("Female minimum capacity must be 0 or greater.")
            .LessThanOrEqualTo(x => x.FemaleMax)
            .When(x => x.FemaleActive)
            .WithMessage("Female minimum capacity must be less than or equal to maximum.");

        RuleFor(x => x.FemaleMax)
            .GreaterThan(0)
            .When(x => x.FemaleActive)
            .WithMessage("Female maximum capacity must be greater than 0 when female section is active.");

        RuleFor(x => x.FemaleWeekDays)
            .GreaterThanOrEqualTo(0).WithMessage("Female weekday price must be 0 or greater.");

        RuleFor(x => x.FemaleWeekEnds)
            .GreaterThanOrEqualTo(0).WithMessage("Female weekend price must be 0 or greater.");

        // Both pricing validation
        RuleFor(x => x.BothWeekDays)
            .GreaterThanOrEqualTo(0).WithMessage("Both weekday price must be 0 or greater.");

        RuleFor(x => x.BothWeekEnds)
            .GreaterThanOrEqualTo(0).WithMessage("Both weekend price must be 0 or greater.");

        // Cross-field validation: at least one gender section must be active
        RuleFor(x => x)
            .Must(x => x.MaleActive || x.FemaleActive)
            .WithMessage("At least one gender section (Male or Female) must be active.")
            .WithName("GenderSections");

        // Gender consistency: if Gender=1 (Male only), male must be active
        RuleFor(x => x.MaleActive)
            .Equal(true)
            .When(x => x.Gender == 1)
            .WithMessage("Male section must be active when gender is set to Male only.");

        // Gender consistency: if Gender=2 (Female only), female must be active
        RuleFor(x => x.FemaleActive)
            .Equal(true)
            .When(x => x.Gender == 2)
            .WithMessage("Female section must be active when gender is set to Female only.");

        // Gender consistency: if Gender=3 (Both), at least one must be active
        RuleFor(x => x)
            .Must(x => x.MaleActive || x.FemaleActive)
            .When(x => x.Gender == 3)
            .WithMessage("At least one gender section must be active when gender is set to Both.")
            .WithName("GenderBothSections");
    }
}

public class HallUpdateDtoValidator : AbstractValidator<HallUpdateDto>
{
    public HallUpdateDtoValidator()
    {
        RuleFor(x => x.ID)
            .GreaterThan(0).WithMessage("Hall ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Hall name is required.")
            .MaximumLength(100).WithMessage("Hall name cannot exceed 100 characters.")
            .MinimumLength(2).WithMessage("Hall name must be at least 2 characters.");

        RuleFor(x => x.CommercialRegistration)
            .InclusiveBetween(100000000, 9999999999)
            .WithMessage("Commercial Registration must be 9 or 10 digits.");

        RuleFor(x => x.Vat)
            .GreaterThanOrEqualTo(0)
            .WithMessage("VAT number must be a positive number.")
            .LessThanOrEqualTo(999999999999999)
            .WithMessage("VAT number must not exceed 15 digits.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9]{9,15}$").WithMessage("Phone number must be 9-15 digits, optionally starting with +.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.WhatsApp)
            .Matches(@"^\+?[0-9]{9,15}$").WithMessage("WhatsApp number must be 9-15 digits, optionally starting with +.")
            .When(x => !string.IsNullOrEmpty(x.WhatsApp));

        // Gender validation
        RuleFor(x => x.Gender)
            .InclusiveBetween(1, 3).WithMessage("Gender must be 1 (Male), 2 (Female), or 3 (Both).");

        // Male capacity validation
        RuleFor(x => x.MaleMin)
            .GreaterThanOrEqualTo(0).WithMessage("Male minimum capacity must be 0 or greater.")
            .LessThanOrEqualTo(x => x.MaleMax)
            .When(x => x.MaleActive)
            .WithMessage("Male minimum capacity must be less than or equal to maximum.");

        RuleFor(x => x.MaleMax)
            .GreaterThan(0)
            .When(x => x.MaleActive)
            .WithMessage("Male maximum capacity must be greater than 0 when male section is active.");

        // Female capacity validation
        RuleFor(x => x.FemaleMin)
            .GreaterThanOrEqualTo(0).WithMessage("Female minimum capacity must be 0 or greater.")
            .LessThanOrEqualTo(x => x.FemaleMax)
            .When(x => x.FemaleActive)
            .WithMessage("Female minimum capacity must be less than or equal to maximum.");

        RuleFor(x => x.FemaleMax)
            .GreaterThan(0)
            .When(x => x.FemaleActive)
            .WithMessage("Female maximum capacity must be greater than 0 when female section is active.");

        // Gender consistency
        RuleFor(x => x.MaleActive)
            .Equal(true)
            .When(x => x.Gender == 1)
            .WithMessage("Male section must be active when gender is set to Male only.");

        RuleFor(x => x.FemaleActive)
            .Equal(true)
            .When(x => x.Gender == 2)
            .WithMessage("Female section must be active when gender is set to Female only.");
    }
}

/// <summary>
/// Validator for HallApprovalRequestDto used in booking approval workflow
/// </summary>
public class HallApprovalRequestDtoValidator : AbstractValidator<HallApprovalRequestDto>
{
    public HallApprovalRequestDtoValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("A rejection reason is required when rejecting a booking.")
            .When(x => !x.Approved);

        RuleFor(x => x.RejectionReason)
            .MaximumLength(500).WithMessage("Rejection reason cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.RejectionReason));
    }
}

/// <summary>
/// Validator for VendorApprovalRequestDto used in vendor approval workflow
/// </summary>
public class VendorApprovalRequestDtoValidator : AbstractValidator<VendorApprovalRequestDto>
{
    public VendorApprovalRequestDtoValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("A rejection reason is required when rejecting a vendor service.")
            .When(x => !x.Approved);

        RuleFor(x => x.RejectionReason)
            .MaximumLength(500).WithMessage("Rejection reason cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.RejectionReason));
    }
}
