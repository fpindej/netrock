using Test.Application.Features.Authentication.Dtos;
using Test.WebApi.Features.Admin.Dtos.OAuthProviders;

namespace Test.WebApi.Features.Admin;

/// <summary>
/// Maps between provider config Application layer DTOs and WebApi response DTOs.
/// </summary>
internal static class OAuthProvidersMapper
{
    /// <summary>
    /// Maps a <see cref="ProviderConfigOutput"/> to an <see cref="OAuthProviderConfigResponse"/>.
    /// </summary>
    public static OAuthProviderConfigResponse ToResponse(this ProviderConfigOutput output) => new()
    {
        Provider = output.Provider,
        DisplayName = output.DisplayName,
        IsEnabled = output.IsEnabled,
        ClientId = output.ClientId,
        HasClientSecret = output.HasClientSecret,
        Source = output.Source,
        UpdatedAt = output.UpdatedAt,
        UpdatedBy = output.UpdatedBy
    };

    /// <summary>
    /// Maps an <see cref="UpdateOAuthProviderRequest"/> to an <see cref="UpsertProviderConfigInput"/>.
    /// </summary>
    public static UpsertProviderConfigInput ToInput(this UpdateOAuthProviderRequest request, string provider) =>
        new(provider, request.IsEnabled, request.ClientId, request.ClientSecret);
}
