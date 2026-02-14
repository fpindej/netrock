using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.Login;

/// <summary>
/// Validates <see cref="RefreshRequest"/> fields at runtime.
/// </summary>
public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    /// <summary>
    /// Initializes validation rules for token refresh requests.
    /// </summary>
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .MaximumLength(500)
            .When(x => x.RefreshToken is not null);
    }
}
