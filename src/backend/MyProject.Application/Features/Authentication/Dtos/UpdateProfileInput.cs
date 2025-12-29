namespace MyProject.Application.Features.Authentication.Dtos;

public record UpdateProfileInput(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Bio,
    string? AvatarUrl
);
