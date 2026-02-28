using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;

/// <summary>
/// Validates <see cref="TwoFactorRecoveryLoginRequest"/> fields at runtime.
/// </summary>
public class TwoFactorRecoveryLoginRequestValidator : AbstractValidator<TwoFactorRecoveryLoginRequest>
{
    /// <summary>
    /// Initializes validation rules for 2FA recovery login requests.
    /// </summary>
    public TwoFactorRecoveryLoginRequestValidator()
    {
        RuleFor(x => x.ChallengeToken)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.RecoveryCode)
            .NotEmpty()
            .MaximumLength(20);
    }
}
