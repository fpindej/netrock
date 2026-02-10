using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.Register;

/// <summary>
/// Validates <see cref="RegisterRequest"/> fields at runtime.
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    private const string PhoneNumberPattern = @"^(\+\d{1,3})? ?\d{6,14}$";

    /// <summary>
    /// Initializes validation rules for registration requests.
    /// </summary>
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(255);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .Matches(PhoneNumberPattern)
            .WithMessage("Phone number must be a valid format (e.g. +420123456789)")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.FirstName)
            .MaximumLength(255);

        RuleFor(x => x.LastName)
            .MaximumLength(255);
    }
}
