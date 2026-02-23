using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyProject.Application.Features.Audit;
using MyProject.Application.Identity;
using MyProject.WebApi.Features.Audit;
using MyProject.WebApi.Features.Audit.Dtos.ListAuditEvents;
using MyProject.WebApi.Features.Users.Dtos;
using MyProject.WebApi.Features.Users.Dtos.DeleteAccount;
using MyProject.WebApi.Features.Users.Dtos.UploadAvatar;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Users;

/// <summary>
/// Controller for managing user profiles and information.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Users")]
public class UsersController(IUserService userService, IAuditService auditService, IUserContext userContext) : ControllerBase
{
    /// <summary>
    /// Gets the current authenticated user's information
    /// </summary>
    /// <returns>User information if authenticated</returns>
    /// <response code="200">Returns user information</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userResult = await userService.GetCurrentUserAsync(cancellationToken);

        if (!userResult.IsSuccess)
        {
            return ProblemFactory.Create(userResult.Error, userResult.ErrorType);
        }

        return Ok(userResult.Value.ToResponse());
    }

    /// <summary>
    /// Updates the current authenticated user's profile information
    /// </summary>
    /// <param name="request">The profile update request</param>
    /// <returns>Updated user information</returns>
    /// <response code="200">Returns updated user information</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> UpdateCurrentUser(
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.UpdateProfileAsync(request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Uploads or replaces the current user's avatar image.
    /// The image is validated, resized to 512x512 max, and stored as WebP.
    /// </summary>
    /// <param name="request">The avatar upload request containing the image file</param>
    /// <returns>Updated user information</returns>
    /// <response code="200">Avatar uploaded successfully</response>
    /// <response code="400">If the file is invalid (too large, wrong format, corrupt)</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPut("me/avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> UploadAvatar(
        [FromForm] UploadAvatarRequest request,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await request.File.CopyToAsync(memoryStream, cancellationToken);
        var imageData = memoryStream.ToArray();

        var result = await userService.UploadAvatarAsync(imageData, request.File.FileName, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Removes the current user's avatar image.
    /// </summary>
    /// <response code="200">Avatar removed successfully</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("me/avatar")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> RemoveAvatar(CancellationToken cancellationToken)
    {
        var result = await userService.RemoveAvatarAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return Ok(result.Value.ToResponse());
    }

    /// <summary>
    /// Gets a user's avatar image by user ID.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>The avatar image file</returns>
    /// <response code="200">Returns the avatar image</response>
    /// <response code="404">If the user has no avatar</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("{userId:guid}/avatar")]
    [ResponseCache(Duration = 300)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetAvatar(Guid userId, CancellationToken cancellationToken)
    {
        var result = await userService.GetAvatarAsync(userId, cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return File(result.Value.Data, result.Value.ContentType);
    }

    /// <summary>
    /// Permanently deletes the current authenticated user's account.
    /// Requires password confirmation. Revokes all tokens and clears auth cookies.
    /// </summary>
    /// <param name="request">The account deletion request containing the user's password</param>
    /// <response code="204">Account successfully deleted</response>
    /// <response code="400">If the password is incorrect or the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("me")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> DeleteAccount(
        [FromBody] DeleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.DeleteAccountAsync(request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ProblemFactory.Create(result.Error, result.ErrorType);
        }

        return NoContent();
    }

    /// <summary>
    /// Gets the current authenticated user's audit activity log.
    /// </summary>
    /// <param name="request">Pagination parameters</param>
    /// <returns>A paginated list of the current user's audit events</returns>
    /// <response code="200">Returns the audit events</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("me/audit")]
    [ProducesResponseType(typeof(ListAuditEventsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ListAuditEventsResponse>> GetMyAuditLog(
        [FromQuery] ListAuditEventsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = userContext.AuthenticatedUserId;
        var result = await auditService.GetUserAuditEventsAsync(userId, request.PageNumber, request.PageSize, cancellationToken);
        return Ok(result.ToResponse());
    }
}
