using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.ForgotPassword;

/// <summary>
/// Validates <see cref="ForgotPasswordRequest"/> fields at runtime.
/// </summary>
public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    /// <summary>
    /// Initializes validation rules for forgot password requests.
    /// </summary>
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);
    }
}
