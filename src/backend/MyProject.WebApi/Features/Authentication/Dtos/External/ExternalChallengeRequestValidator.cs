using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.External;

/// <summary>
/// Validates <see cref="ExternalChallengeRequest"/> fields at runtime.
/// </summary>
public class ExternalChallengeRequestValidator : AbstractValidator<ExternalChallengeRequest>
{
    /// <summary>
    /// Initializes validation rules for external challenge requests.
    /// </summary>
    public ExternalChallengeRequestValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.RedirectUri)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("RedirectUri must be a valid absolute URI.");
    }
}
