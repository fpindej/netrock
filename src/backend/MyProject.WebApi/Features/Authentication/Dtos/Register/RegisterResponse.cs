using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.Register;

/// <summary>
/// Represents the response returned after successful user registration.
/// </summary>
[UsedImplicitly]
public class RegisterResponse
{
    /// <summary>
    /// The unique identifier of the newly created user.
    /// </summary>
    public Guid Id { get; [UsedImplicitly] init; }
}
