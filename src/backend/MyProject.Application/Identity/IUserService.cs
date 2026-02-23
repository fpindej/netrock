using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Features.FileStorage.Dtos;
using MyProject.Application.Identity.Dtos;
using MyProject.Shared;

namespace MyProject.Application.Identity;

/// <summary>
/// Provides operations for retrieving and updating user profile information.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets the current authenticated user information.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A Result containing the user if authenticated, or failure if not.</returns>
    Task<Result<UserOutput>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the current user's profile information.
    /// </summary>
    /// <param name="input">The profile update input.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A Result containing the updated user if successful, or failure if not.</returns>
    Task<Result<UserOutput>> UpdateProfileAsync(UpdateProfileInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the current user's account after password confirmation.
    /// Revokes all tokens, clears auth cookies, and permanently removes the user.
    /// </summary>
    /// <param name="input">The account deletion input containing the user's password for confirmation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DeleteAccountAsync(DeleteAccountInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the roles for a specific user.
    /// </summary>
    /// <param name="userId">The user ID to get roles for.</param>
    /// <returns>A list of role names.</returns>
    Task<IList<string>> GetUserRolesAsync(Guid userId);

    /// <summary>
    /// Uploads and processes an avatar image for the current user.
    /// </summary>
    /// <param name="imageData">The raw uploaded file bytes.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A Result containing the updated user output.</returns>
    Task<Result<UserOutput>> UploadAvatarAsync(byte[] imageData, string fileName, CancellationToken ct);

    /// <summary>
    /// Removes the current user's avatar image.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A Result containing the updated user output.</returns>
    Task<Result<UserOutput>> RemoveAvatarAsync(CancellationToken ct);

    /// <summary>
    /// Gets the avatar image for a specific user.
    /// </summary>
    /// <param name="userId">The user ID whose avatar to retrieve.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A Result containing the file data and content type.</returns>
    Task<Result<FileDownloadOutput>> GetAvatarAsync(Guid userId, CancellationToken ct);
}
