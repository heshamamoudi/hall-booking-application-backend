using FluentValidation;
using HallApp.Application.DTOs.Organization;

namespace HallApp.Application.Validators;

public class AddTeamMemberDtoValidator : AbstractValidator<AddTeamMemberDto>
{
    public AddTeamMemberDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => role is "HallManager" or "VendorManager")
            .WithMessage("Role must be 'HallManager' or 'VendorManager'.");

        RuleFor(x => x.PasswordOption)
            .NotEmpty().WithMessage("Password option is required.")
            .Must(option => option is "set" or "invite")
            .WithMessage("Password option must be 'set' or 'invite'.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required when password option is 'set'.")
            .When(x => x.PasswordOption == "set");

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.")
            .When(x => x.PasswordOption == "set" && !string.IsNullOrWhiteSpace(x.Password));
    }
}

/// <summary>
/// Validates UpdateOrganizationDto business registration fields.
/// All fields are optional (empty string = not provided), but when provided they must match
/// Saudi Arabia business registration formats.
/// </summary>
public class UpdateOrganizationDtoValidator : AbstractValidator<UpdateOrganizationDto>
{
    public UpdateOrganizationDtoValidator()
    {
        // Name: if provided, must be 2-200 characters
        RuleFor(x => x.Name)
            .MinimumLength(2).WithMessage("Organization name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Organization name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        // Commercial Registration Number: exactly 10 digits when provided
        RuleFor(x => x.CommercialRegistrationNumber)
            .Matches(@"^\d{10}$").WithMessage("Commercial Registration Number must be exactly 10 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.CommercialRegistrationNumber));

        // VAT Number: exactly 15 digits, starts with '3', ends with '3' (Saudi ZATCA format)
        RuleFor(x => x.VatNumber)
            .Matches(@"^3\d{13}3$").WithMessage("VAT Number must be 15 digits, starting and ending with '3'.")
            .When(x => !string.IsNullOrWhiteSpace(x.VatNumber));

        // Legal Name: max 200 characters
        RuleFor(x => x.LegalName)
            .MaximumLength(200).WithMessage("Legal name cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.LegalName));

        // Address fields
        RuleFor(x => x.BuildingNumber)
            .MaximumLength(10).WithMessage("Building number cannot exceed 10 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.BuildingNumber));

        RuleFor(x => x.StreetName)
            .MaximumLength(100).WithMessage("Street name cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.StreetName));

        RuleFor(x => x.District)
            .MaximumLength(100).WithMessage("District cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.District));

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.City));

        RuleFor(x => x.PostalCode)
            .Matches(@"^\d{5}$").WithMessage("Postal code must be exactly 5 digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.PostalCode));

        RuleFor(x => x.CountryCode)
            .Length(2).WithMessage("Country code must be exactly 2 characters (ISO 3166-1 alpha-2).")
            .Matches(@"^[A-Z]{2}$").WithMessage("Country code must be 2 uppercase letters (ISO 3166-1 alpha-2).")
            .When(x => !string.IsNullOrWhiteSpace(x.CountryCode));

        // Email: standard email validation when provided
        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        // Phone: Saudi phone format (starts with +966 or 05, 9-15 chars including optional +)
        RuleFor(x => x.Phone)
            .Matches(@"^(\+966|05)\d{8,12}$").WithMessage("Phone must be a valid Saudi number (e.g., +966501234567 or 0501234567).")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        // WhatsApp: same phone format
        RuleFor(x => x.WhatsApp)
            .Matches(@"^(\+966|05)\d{8,12}$").WithMessage("WhatsApp must be a valid Saudi number (e.g., +966501234567 or 0501234567).")
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsApp));

        // Website: basic URL validation
        RuleFor(x => x.Website)
            .MaximumLength(500).WithMessage("Website URL cannot exceed 500 characters.")
            .Matches(@"^https?://").WithMessage("Website must start with http:// or https://.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        // Bank Name
        RuleFor(x => x.BankName)
            .MaximumLength(100).WithMessage("Bank name cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.BankName));

        // Bank Account Number
        RuleFor(x => x.BankAccountNumber)
            .MaximumLength(30).WithMessage("Bank account number cannot exceed 30 characters.")
            .Matches(@"^\d+$").WithMessage("Bank account number must contain only digits.")
            .When(x => !string.IsNullOrWhiteSpace(x.BankAccountNumber));

        // IBAN: Saudi IBAN format = SA + 22 alphanumeric characters (total 24)
        RuleFor(x => x.BankIban)
            .Matches(@"^SA\d{2}[A-Za-z0-9]{18}$").WithMessage("IBAN must be in Saudi format: SA followed by 2 check digits and 18 alphanumeric characters (24 total).")
            .When(x => !string.IsNullOrWhiteSpace(x.BankIban));
    }
}
