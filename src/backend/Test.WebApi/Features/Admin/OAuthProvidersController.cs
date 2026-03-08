using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Test.Application.Features.Authentication;
using Test.Application.Identity;
using Test.Application.Identity.Constants;
using Test.Shared;
using Test.WebApi.Authorization;
using Test.WebApi.Features.Admin.Dtos.OAuthProviders;
using Test.WebApi.Shared;

namespace Test.WebApi.Features.Admin;

/// <summary>
/// Administrative endpoints for managing OAuth provider configurations.
/// </summary>
[Route("api/v1/admin")]
[Tags("OAuthProviders")]
public class OAuthProvidersController(
    IProviderConfigService providerConfigService,
    IUserContext userContext) : ApiController
{
    /// <summary>
    /// Gets all known OAuth providers with their configuration state.
    /// </summary>
    /// <returns>A list of provider configurations</returns>
    /// <response code="200">Returns the list of provider configurations</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    [HttpGet("oauth-providers")]
    [RequirePermission(AppPermissions.OAuthProviders.View)]
    [ProducesResponseType(typeof(IReadOnlyList<OAuthProviderConfigResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<OAuthProviderConfigResponse>>> ListProviders(
        CancellationToken cancellationToken)
    {
        var configs = await providerConfigService.GetAllAsync(cancellationToken);
        return Ok(configs.Select(c => c.ToResponse()).ToList());
    }

    /// <summary>
    /// Creates or updates an OAuth provider's configuration.
    /// </summary>
    /// <param name="provider">The provider identifier (e.g. "Google", "GitHub")</param>
    /// <param name="request">The provider configuration to apply</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Provider configuration updated successfully</response>
    /// <response code="400">If the request is invalid or the provider is unknown</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="429">If too many requests have been made</response>
    [HttpPut("oauth-providers/{provider:providerName}")]
    [RequirePermission(AppPermissions.OAuthProviders.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> UpdateProvider(
        string provider,
        [FromBody] UpdateOAuthProviderRequest request,
        CancellationToken cancellationToken)
    {
        var configs = await providerConfigService.GetAllAsync(cancellationToken);
        var config = configs.FirstOrDefault(
            c => string.Equals(c.Provider, provider, StringComparison.OrdinalIgnoreCase));

        if (config is null)
        {
            return ProblemFactory.Create(ErrorMessages.ExternalAuth.UnknownProvider, ErrorType.Validation);
        }

        if (request.IsEnabled && request.ClientSecret is null && !config.HasClientSecret)
        {
            return ProblemFactory.Create(ErrorMessages.ExternalAuth.ClientSecretRequired, ErrorType.Validation);
        }

        var callerUserId = userContext.AuthenticatedUserId;
        var result = await providerConfigService.UpsertAsync(
            callerUserId, request.ToInput(provider), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }

    /// <summary>
    /// Tests whether the stored credentials for a provider are valid.
    /// </summary>
    /// <param name="provider">The provider identifier (e.g. "Google", "GitHub")</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Credentials are valid</response>
    /// <response code="400">If the credentials are invalid or the provider is not configured</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="429">If too many requests have been made</response>
    [HttpPost("oauth-providers/{provider:providerName}/test")]
    [RequirePermission(AppPermissions.OAuthProviders.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> TestConnection(
        string provider,
        CancellationToken cancellationToken)
    {
        var callerUserId = userContext.AuthenticatedUserId;
        var result = await providerConfigService.TestConnectionAsync(
            callerUserId, provider, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }
}
