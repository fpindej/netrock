using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;

/// <summary>
/// Validates <see cref="TwoFactorDisableRequest"/> fields at runtime.
/// </summary>
public class TwoFactorDisableRequestValidator : AbstractValidator<TwoFactorDisableRequest>
{
    /// <summary>
    /// Initializes validation rules for 2FA disable requests.
    /// </summary>
    public TwoFactorDisableRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(255);
    }
}
