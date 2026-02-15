using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.VerifyEmail;

/// <summary>
/// Validates <see cref="VerifyEmailRequest"/> fields at runtime.
/// </summary>
public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    /// <summary>
    /// Initializes validation rules for email verification requests.
    /// </summary>
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
