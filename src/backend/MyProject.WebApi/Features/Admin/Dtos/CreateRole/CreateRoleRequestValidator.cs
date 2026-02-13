using FluentValidation;

namespace MyProject.WebApi.Features.Admin.Dtos.CreateRole;

/// <summary>
/// Validates <see cref="CreateRoleRequest"/> fields at runtime.
/// </summary>
public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    /// <summary>
    /// Initializes validation rules for role creation requests.
    /// </summary>
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Description)
            .MaximumLength(200);
    }
}
