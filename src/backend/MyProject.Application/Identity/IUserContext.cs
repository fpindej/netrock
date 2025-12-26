namespace MyProject.Application.Identity;

public interface IUserContext
{
    Guid? UserId { get; }
    string? Email { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
