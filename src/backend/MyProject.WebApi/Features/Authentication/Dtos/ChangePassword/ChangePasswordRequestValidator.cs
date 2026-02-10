using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.ChangePassword;

/// <summary>
/// Validates <see cref="ChangePasswordRequest"/> fields at runtime.
/// </summary>
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    /// <summary>
    /// Initializes validation rules for change password requests.
    /// </summary>
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(255)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must be different from the current password.");
    }
}
