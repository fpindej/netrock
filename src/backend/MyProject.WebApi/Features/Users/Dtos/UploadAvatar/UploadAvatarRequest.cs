using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Users.Dtos.UploadAvatar;

/// <summary>
/// Represents a request to upload an avatar image.
/// </summary>
[UsedImplicitly]
public class UploadAvatarRequest
{
    /// <summary>
    /// The avatar image file.
    /// </summary>
    public IFormFile File { get; init; } = default!;
}
