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
            .NotNull();

        RuleForEach(x => x.Permissions)
            .NotEmpty()
            .MaximumLength(100);
    }
}
