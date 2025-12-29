namespace MyProject.Application.Features.Authentication.Dtos;

public record UpdateProfileInput(
    string? FirstName,
    string? LastName,
    string? Bio,
    string? AvatarUrl
);
