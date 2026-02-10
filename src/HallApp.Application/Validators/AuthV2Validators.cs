using FluentValidation;
using HallApp.Application.DTOs.Auth;

namespace HallApp.Application.Validators;

public class CheckEmailRequestDtoValidator : AbstractValidator<CheckEmailRequestDto>
{
    public CheckEmailRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

public class LoginV2DtoValidator : AbstractValidator<LoginV2Dto>
{
    public LoginV2DtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
    }
}

public class RegisterV2DtoValidator : AbstractValidator<RegisterV2Dto>
{
    public RegisterV2DtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MinimumLength(2).WithMessage("First name must be at least 2 characters")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MinimumLength(2).WithMessage("Last name must be at least 2 characters")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?\d{10,15}$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

public class UpdateProfileV2DtoValidator : AbstractValidator<UpdateProfileV2Dto>
{
    public UpdateProfileV2DtoValidator()
    {
        RuleFor(x => x.FirstName)
            .MinimumLength(2).WithMessage("First name must be at least 2 characters")
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MinimumLength(2).WithMessage("Last name must be at least 2 characters")
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?\d{10,15}$").WithMessage("Invalid phone number format")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Gender)
            .Must(g => g == null || new[] { "Male", "Female", "Other", "NotSpecified" }.Contains(g))
            .WithMessage("Gender must be Male, Female, Other, or NotSpecified");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.UtcNow.AddYears(-13)).WithMessage("You must be at least 13 years old")
            .GreaterThan(DateTime.UtcNow.AddYears(-120)).WithMessage("Invalid date of birth")
            .When(x => x.DateOfBirth.HasValue);
    }
}

public class ChangePasswordV2DtoValidator : AbstractValidator<ChangePasswordV2Dto>
{
    public ChangePasswordV2DtoValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
    }
}
