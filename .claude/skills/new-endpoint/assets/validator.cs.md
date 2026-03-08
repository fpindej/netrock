# FluentValidation Validator Template

```csharp
using FluentValidation;

namespace MyProject.WebApi.Features.{Feature}.Dtos.{Operation};

/// <summary>
/// Validates <see cref="{Operation}Request"/> fields at runtime.
/// </summary>
public class {Operation}RequestValidator : AbstractValidator<{Operation}Request>
{
    /// <summary>
    /// Initializes validation rules for {operation} requests.
    /// </summary>
    public {Operation}RequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        // Optional field
        // RuleFor(x => x.Description)
        //     .MaximumLength(500)
        //     .When(x => !string.IsNullOrEmpty(x.Description));

        // New password
        // RuleFor(x => x.Password)
        //     .MinimumLength(6)
        //     .Matches("[a-z]").WithMessage("Must contain a lowercase letter.")
        //     .Matches("[A-Z]").WithMessage("Must contain an uppercase letter.")
        //     .Matches("[0-9]").WithMessage("Must contain a digit.");

        // Existing password (just check non-empty)
        // RuleFor(x => x.CurrentPassword)
        //     .NotEmpty()
        //     .MaximumLength(255);

        // URL field
        // RuleFor(x => x.Website)
        //     .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        //         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        //     .When(x => !string.IsNullOrEmpty(x.Website))
        //     .WithMessage("Must be a valid HTTP or HTTPS URL.");

        // Shared patterns from ValidationConstants
        // RuleFor(x => x.Phone)
        //     .Matches(ValidationConstants.PhonePattern)
        //     .When(x => !string.IsNullOrEmpty(x.Phone));
    }
}
```

## Rules

- Co-locate with the request DTO in the same directory
- Auto-discovered from WebApi assembly - no manual registration
- `.When(x => !string.IsNullOrEmpty(x.Field))` for optional fields
- Extract shared patterns to `ValidationConstants.cs`
