using FluentValidation;

namespace MyProject.WebApi.Features.Admin.Dtos.SetPermissions;

/// <summary>
/// Validates <see cref="SetPermissionsRequest"/> fields at runtime.
/// </summary>
public class SetPermissionsRequestValidator : AbstractValidator<SetPermissionsRequest>
{
    /// <summary>
    /// Initializes validation rules for set permissions requests.
    /// </summary>
    public SetPermissionsRequestValidator()
    {
        RuleFor(x => x.Permissions)
            .NotNull()
            .Must(p => p.Count <= 50)
            .WithMessage("Cannot set more than 50 permissions at once.");

        RuleForEach(x => x.Permissions)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-z][a-z0-9_.]*$")
            .WithMessage("Permission values must be lowercase alphanumeric with dots or underscores.");
    }
}
