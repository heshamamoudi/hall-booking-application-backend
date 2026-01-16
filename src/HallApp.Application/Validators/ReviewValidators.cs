using FluentValidation;
using HallApp.Application.DTOs.Review;

namespace HallApp.Application.Validators;

public class CreateReviewDtoValidator : AbstractValidator<CreateReviewDto>
{
    public CreateReviewDtoValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID is required");

        RuleFor(x => x)
            .Must(x => x.HallId.HasValue || x.VendorId.HasValue)
            .WithMessage("Either Hall ID or Vendor ID must be provided");
    }
}

public class UpdateReviewDtoValidator : AbstractValidator<UpdateReviewDto>
{
    public UpdateReviewDtoValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters");
    }
}
