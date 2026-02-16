using FluentValidation;

namespace MyProject.WebApi.Features.Admin.Dtos.CreateUser;

/// <summary>
/// Validates <see cref="CreateUserRequest"/> fields at runtime.
/// </summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    /// <summary>
    /// Initializes validation rules for user creation requests.
    /// </summary>
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.FirstName)
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .MaximumLength(100);
    }
}
