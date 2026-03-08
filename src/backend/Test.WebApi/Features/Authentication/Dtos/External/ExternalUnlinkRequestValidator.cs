using FluentValidation;

namespace Test.WebApi.Features.Authentication.Dtos.External;

/// <summary>
/// Validates <see cref="ExternalUnlinkRequest"/> fields at runtime.
/// </summary>
public class ExternalUnlinkRequestValidator : AbstractValidator<ExternalUnlinkRequest>
{
    /// <summary>
    /// Initializes validation rules for external unlink requests.
    /// </summary>
    public ExternalUnlinkRequestValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .MaximumLength(32);
    }
}
