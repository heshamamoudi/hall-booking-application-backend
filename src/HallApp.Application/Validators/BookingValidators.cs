using FluentValidation;
using HallApp.Application.DTOs.Booking.Registers;

namespace HallApp.Application.Validators;

public class BookingRegisterDtoValidator : AbstractValidator<BookingRegisterDto>
{
    public BookingRegisterDtoValidator()
    {
        RuleFor(x => x.HallId)
            .GreaterThan(0).WithMessage("Hall ID is required");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("Customer ID is required");

        RuleFor(x => x.BookingDate)
            .NotEmpty().WithMessage("Booking date is required")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Booking date cannot be in the past");

        RuleFor(x => x.TotalPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Total price must be positive");

        RuleFor(x => x.Tax)
            .GreaterThanOrEqualTo(0).WithMessage("Tax must be positive");

        RuleFor(x => x.Discount)
            .InclusiveBetween(0, 100).WithMessage("Discount must be between 0 and 100");

        RuleFor(x => x.PaymentMethod)
            .MaximumLength(50).WithMessage("Payment method cannot exceed 50 characters");

        RuleFor(x => x.Coupon)
            .MaximumLength(50).WithMessage("Coupon code cannot exceed 50 characters");

        RuleFor(x => x.Comments)
            .MaximumLength(500).WithMessage("Comments cannot exceed 500 characters");
    }
}
