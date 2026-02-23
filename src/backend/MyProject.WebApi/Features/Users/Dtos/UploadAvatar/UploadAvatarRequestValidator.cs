using FluentValidation;

namespace MyProject.WebApi.Features.Users.Dtos.UploadAvatar;

/// <summary>
/// Validates <see cref="UploadAvatarRequest"/> fields at runtime.
/// </summary>
public class UploadAvatarRequestValidator : AbstractValidator<UploadAvatarRequest>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    /// <summary>
    /// Initializes validation rules for avatar upload requests.
    /// </summary>
    public UploadAvatarRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("A file is required.");

        RuleFor(x => x.File.Length)
            .GreaterThan(0)
            .WithMessage("The file must not be empty.")
            .LessThanOrEqualTo(MaxFileSizeBytes)
            .WithMessage("The file must not exceed 5 MB.")
            .When(x => x.File is not null);

        RuleFor(x => x.File.ContentType)
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage("Unsupported file type. Allowed: JPEG, PNG, WebP, GIF.")
            .When(x => x.File is not null);
    }
}
