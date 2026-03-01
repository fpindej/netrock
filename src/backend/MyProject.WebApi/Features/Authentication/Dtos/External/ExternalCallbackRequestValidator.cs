using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.External;

/// <summary>
/// Validates <see cref="ExternalCallbackRequest"/> fields at runtime.
/// </summary>
public class ExternalCallbackRequestValidator : AbstractValidator<ExternalCallbackRequest>
{
    /// <summary>
    /// Initializes validation rules for external callback requests.
    /// </summary>
    public ExternalCallbackRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(2048);

        RuleFor(x => x.State)
            .NotEmpty()
            .MaximumLength(512);
    }
}
