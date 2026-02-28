using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;

/// <summary>
/// Validates <see cref="TwoFactorLoginRequest"/> fields at runtime.
/// </summary>
public class TwoFactorLoginRequestValidator : AbstractValidator<TwoFactorLoginRequest>
{
    /// <summary>
    /// Initializes validation rules for 2FA login requests.
    /// </summary>
    public TwoFactorLoginRequestValidator()
    {
        RuleFor(x => x.ChallengeToken)
            .NotEmpty();

        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(6)
            .Matches(@"^\d{6}$").WithMessage("Code must be exactly 6 digits.");
    }
}
