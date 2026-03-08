using FluentValidation;

namespace Test.WebApi.Features.Admin.Dtos.OAuthProviders;

/// <summary>
/// Validates <see cref="UpdateOAuthProviderRequest"/> fields at runtime.
/// </summary>
public class UpdateOAuthProviderRequestValidator : AbstractValidator<UpdateOAuthProviderRequest>
{
    /// <summary>
    /// Initializes validation rules for OAuth provider update requests.
    /// </summary>
    public UpdateOAuthProviderRequestValidator()
    {
        RuleFor(x => x.ClientId)
            .NotEmpty()
            .When(x => x.IsEnabled)
            .WithMessage("Client ID is required when the provider is enabled.");

        RuleFor(x => x.ClientId)
            .MaximumLength(256);

        RuleFor(x => x.ClientSecret)
            .MaximumLength(512);
    }
}
