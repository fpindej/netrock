using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.SetPassword;

/// <summary>
/// Validates <see cref="SetPasswordRequest"/> fields at runtime.
/// </summary>
public class SetPasswordRequestValidator : AbstractValidator<SetPasswordRequest>
{
    /// <summary>
    /// Initializes validation rules for set password requests.
    /// </summary>
    public SetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(255)
            .Matches("[a-z]")
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one digit.");
    }
}
