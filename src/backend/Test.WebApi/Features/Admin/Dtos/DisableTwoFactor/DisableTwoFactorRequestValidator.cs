using FluentValidation;

namespace Test.WebApi.Features.Admin.Dtos.DisableTwoFactor;

/// <summary>
/// Validates <see cref="DisableTwoFactorRequest"/> fields at runtime.
/// </summary>
public class DisableTwoFactorRequestValidator : AbstractValidator<DisableTwoFactorRequest>
{
    /// <summary>
    /// Initializes validation rules for disable two-factor requests.
    /// </summary>
    public DisableTwoFactorRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}
