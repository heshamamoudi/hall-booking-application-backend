using FluentValidation;
using HallApp.Application.DTOs.Halls.Hall;

namespace HallApp.Application.Validators;

public class HallCreateDtoValidator : AbstractValidator<HallCreateDto>
{
    public HallCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Hall name is required")
            .MaximumLength(100).WithMessage("Hall name cannot exceed 100 characters");

        RuleFor(x => x.CommercialRegistration)
            .GreaterThan(0).WithMessage("Commercial registration is required");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required")
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Invalid phone format");

        RuleFor(x => x.MaleMin)
            .LessThanOrEqualTo(x => x.MaleMax)
            .When(x => x.MaleActive)
            .WithMessage("Male minimum capacity must be less than or equal to maximum");

        RuleFor(x => x.FemaleMin)
            .LessThanOrEqualTo(x => x.FemaleMax)
            .When(x => x.FemaleActive)
            .WithMessage("Female minimum capacity must be less than or equal to maximum");

        RuleFor(x => x.MaleWeekDays)
            .GreaterThanOrEqualTo(0).WithMessage("Male weekday price must be positive");

        RuleFor(x => x.MaleWeekEnds)
            .GreaterThanOrEqualTo(0).WithMessage("Male weekend price must be positive");

        RuleFor(x => x.FemaleWeekDays)
            .GreaterThanOrEqualTo(0).WithMessage("Female weekday price must be positive");

        RuleFor(x => x.FemaleWeekEnds)
            .GreaterThanOrEqualTo(0).WithMessage("Female weekend price must be positive");

        RuleFor(x => x.Gender)
            .InclusiveBetween(1, 3).WithMessage("Gender must be 1 (Male), 2 (Female), or 3 (Both)");
    }
}

public class HallUpdateDtoValidator : AbstractValidator<HallUpdateDto>
{
    public HallUpdateDtoValidator()
    {
        RuleFor(x => x.ID)
            .GreaterThan(0).WithMessage("Hall ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Hall name is required")
            .MaximumLength(100).WithMessage("Hall name cannot exceed 100 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[0-9]{10,15}$").WithMessage("Invalid phone format")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.MaleMin)
            .LessThanOrEqualTo(x => x.MaleMax)
            .When(x => x.MaleActive)
            .WithMessage("Male minimum capacity must be less than or equal to maximum");

        RuleFor(x => x.FemaleMin)
            .LessThanOrEqualTo(x => x.FemaleMax)
            .When(x => x.FemaleActive)
            .WithMessage("Female minimum capacity must be less than or equal to maximum");

        RuleFor(x => x.Gender)
            .InclusiveBetween(1, 3).WithMessage("Gender must be 1 (Male), 2 (Female), or 3 (Both)");
    }
}
