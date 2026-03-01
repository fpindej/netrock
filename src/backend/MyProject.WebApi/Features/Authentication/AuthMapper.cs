using MyProject.Application.Features.Authentication.Dtos;
using MyProject.WebApi.Features.Authentication.Dtos.ChangePassword;
using MyProject.WebApi.Features.Authentication.Dtos.External;
using MyProject.WebApi.Features.Authentication.Dtos.Login;
using MyProject.WebApi.Features.Authentication.Dtos.Register;
using MyProject.WebApi.Features.Authentication.Dtos.ResetPassword;
using MyProject.WebApi.Features.Authentication.Dtos.SetPassword;
using MyProject.WebApi.Features.Authentication.Dtos.TwoFactor;
using MyProject.WebApi.Features.Authentication.Dtos.VerifyEmail;

namespace MyProject.WebApi.Features.Authentication;

/// <summary>
/// Maps between authentication WebApi DTOs and Application layer DTOs.
/// </summary>
internal static class AuthMapper
{
    /// <summary>
    /// Maps a <see cref="RegisterRequest"/> to a <see cref="RegisterInput"/>.
    /// </summary>
    public static RegisterInput ToRegisterInput(this RegisterRequest request) =>
        new(
            Email: request.Email,
            Password: request.Password,
            FirstName: request.FirstName,
            LastName: request.LastName,
            PhoneNumber: request.PhoneNumber
        );

    /// <summary>
    /// Maps an <see cref="AuthenticationOutput"/> to an <see cref="AuthenticationResponse"/>.
    /// </summary>
    public static AuthenticationResponse ToResponse(this AuthenticationOutput output) =>
        new()
        {
            AccessToken = output.AccessToken,
            RefreshToken = output.RefreshToken
        };

    /// <summary>
    /// Maps a <see cref="LoginOutput"/> to an <see cref="AuthenticationResponse"/>.
    /// When 2FA is required, tokens are empty and challenge fields are populated.
    /// </summary>
    public static AuthenticationResponse ToResponse(this LoginOutput output) =>
        output.Tokens is { } tokens
            ? new AuthenticationResponse
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken
            }
            : new AuthenticationResponse
            {
                RequiresTwoFactor = true,
                ChallengeToken = output.ChallengeToken
            };

    /// <summary>
    /// Maps a <see cref="ChangePasswordRequest"/> to a <see cref="ChangePasswordInput"/>.
    /// </summary>
    public static ChangePasswordInput ToChangePasswordInput(this ChangePasswordRequest request) =>
        new(
            CurrentPassword: request.CurrentPassword,
            NewPassword: request.NewPassword
        );

    /// <summary>
    /// Maps a <see cref="ResetPasswordRequest"/> to a <see cref="ResetPasswordInput"/>.
    /// </summary>
    public static ResetPasswordInput ToResetPasswordInput(this ResetPasswordRequest request) =>
        new(
            Token: request.Token,
            NewPassword: request.NewPassword
        );

    /// <summary>
    /// Maps a <see cref="VerifyEmailRequest"/> to a <see cref="VerifyEmailInput"/>.
    /// </summary>
    public static VerifyEmailInput ToVerifyEmailInput(this VerifyEmailRequest request) =>
        new(
            Token: request.Token
        );

    /// <summary>
    /// Maps a <see cref="TwoFactorSetupOutput"/> to a <see cref="TwoFactorSetupResponse"/>.
    /// </summary>
    public static TwoFactorSetupResponse ToResponse(this TwoFactorSetupOutput output) =>
        new()
        {
            SharedKey = output.SharedKey,
            AuthenticatorUri = output.AuthenticatorUri
        };

    /// <summary>
    /// Maps a <see cref="TwoFactorVerifySetupOutput"/> to a <see cref="TwoFactorVerifySetupResponse"/>.
    /// </summary>
    public static TwoFactorVerifySetupResponse ToResponse(this TwoFactorVerifySetupOutput output) =>
        new()
        {
            RecoveryCodes = output.RecoveryCodes
        };

    /// <summary>
    /// Maps an <see cref="ExternalChallengeRequest"/> to an <see cref="ExternalChallengeInput"/>.
    /// </summary>
    public static ExternalChallengeInput ToChallengeInput(this ExternalChallengeRequest request) =>
        new(Provider: request.Provider, RedirectUri: request.RedirectUri);

    /// <summary>
    /// Maps an <see cref="ExternalCallbackRequest"/> to an <see cref="ExternalCallbackInput"/>.
    /// </summary>
    public static ExternalCallbackInput ToCallbackInput(this ExternalCallbackRequest request) =>
        new(Code: request.Code, State: request.State);

    /// <summary>
    /// Maps an <see cref="ExternalCallbackOutput"/> to an <see cref="ExternalCallbackResponse"/>.
    /// </summary>
    public static ExternalCallbackResponse ToResponse(this ExternalCallbackOutput output) =>
        new()
        {
            AccessToken = output.Tokens?.AccessToken ?? string.Empty,
            RefreshToken = output.Tokens?.RefreshToken ?? string.Empty,
            IsNewUser = output.IsNewUser,
            Provider = output.Provider,
            IsLinkOnly = output.IsLinkOnly
        };

    /// <summary>
    /// Maps an <see cref="ExternalProviderInfo"/> to an <see cref="ExternalProviderResponse"/>.
    /// </summary>
    public static ExternalProviderResponse ToResponse(this ExternalProviderInfo info) =>
        new()
        {
            Name = info.Name,
            DisplayName = info.DisplayName
        };

    /// <summary>
    /// Maps a <see cref="SetPasswordRequest"/> to a <see cref="SetPasswordInput"/>.
    /// </summary>
    public static SetPasswordInput ToSetPasswordInput(this SetPasswordRequest request) =>
        new(NewPassword: request.NewPassword);
}
