using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using MyProject.Application.Features.Authentication.Dtos;

namespace MyProject.WebApi.Features.Authentication.Dtos.Profile;

/// <summary>
/// Represents a request to update the user's profile information.
/// </summary>
[UsedImplicitly]
public class UpdateProfileRequest
{
    /// <summary>
    /// The first name of the user.
    /// </summary>
    [MaxLength(255)]
    [Description("The first name of the user (optional), maximum 255 characters")]
    public string? FirstName { get; init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [MaxLength(255)]
    [Description("The last name of the user (optional), maximum 255 characters")]
    public string? LastName { get; init; }

    /// <summary>
    /// A short biography or description of the user.
    /// </summary>
    [MaxLength(1000)]
    [Description("A short biography or description of the user (optional), maximum 1000 characters")]
    public string? Bio { get; init; }

    /// <summary>
    /// The URL to the user's avatar image.
    /// </summary>
    [MaxLength(500)]
    [Url]
    [Description("The URL to the user's avatar image (optional), must be a valid URL")]
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// Converts the request to an application layer input.
    /// </summary>
    public UpdateProfileInput ToInput() => new(FirstName, LastName, Bio, AvatarUrl);
}
