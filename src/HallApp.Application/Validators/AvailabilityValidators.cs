using FluentValidation;
using HallApp.Application.DTOs.Halls.Availability;

namespace HallApp.Application.Validators;

/// <summary>
/// Validator for BlockDateRequestDto.
/// Ensures date ranges are valid and reason is provided.
/// </summary>
public class BlockDateRequestValidator : AbstractValidator<BlockDateRequestDto>
{
    public BlockDateRequestValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Start date must be today or in the future.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("End date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be greater than or equal to start date.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required for blocking dates.")
            .MinimumLength(1).WithMessage("Reason must be at least 1 character.")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
