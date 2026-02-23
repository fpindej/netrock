using MyProject.WebApi.Features.Admin;
using MyProject.WebApi.Features.Admin.Dtos;
using MyProject.WebApi.Features.Admin.Dtos.ListUsers;

namespace MyProject.Api.Tests.Features.Admin;

public class AdminMapperPiiTests
{
    private static AdminUserResponse CreateUser(Guid id, string email = "john@example.com", string? phone = "+420123456789") => new()
    {
        Id = id,
        Username = email,
        Email = email,
        FirstName = "John",
        LastName = "Doe",
        PhoneNumber = phone,
        Bio = "Test bio",
        HasAvatar = true,
        Roles = ["Admin"],
        EmailConfirmed = true,
        LockoutEnabled = false,
        LockoutEnd = null,
        AccessFailedCount = 0,
        IsLockedOut = false
    };

    [Fact]
    public void WithMaskedPii_SingleUser_ShouldMaskEmailUsernameAndPhone()
    {
        var user = CreateUser(Guid.NewGuid());

        var masked = user.WithMaskedPii();

        Assert.Equal("j***@e***.com", masked.Email);
        Assert.Equal("j***@e***.com", masked.Username);
        Assert.Equal("***", masked.PhoneNumber);
    }

    [Fact]
    public void WithMaskedPii_SingleUser_ShouldPreserveNonPiiFields()
    {
        var user = CreateUser(Guid.NewGuid());

        var masked = user.WithMaskedPii();

        Assert.Equal(user.Id, masked.Id);
        Assert.Equal("John", masked.FirstName);
        Assert.Equal("Doe", masked.LastName);
        Assert.Equal("Test bio", masked.Bio);
        Assert.True(masked.HasAvatar);
        Assert.Equal(["Admin"], masked.Roles);
        Assert.True(masked.EmailConfirmed);
        Assert.False(masked.LockoutEnabled);
        Assert.Null(masked.LockoutEnd);
        Assert.Equal(0, masked.AccessFailedCount);
        Assert.False(masked.IsLockedOut);
    }

    [Fact]
    public void WithMaskedPii_SingleUser_NullPhone_ShouldRemainNull()
    {
        var user = CreateUser(Guid.NewGuid(), phone: null);

        var masked = user.WithMaskedPii();

        Assert.Null(masked.PhoneNumber);
    }

    [Fact]
    public void WithMaskedPii_List_ShouldExemptCallerOwnEntry()
    {
        var callerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        var response = new ListUsersResponse
        {
            Items = [CreateUser(callerId), CreateUser(otherId, "other@test.org")],
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        var masked = response.WithMaskedPii(callerId);

        // Caller's entry should be unmasked
        var callerEntry = masked.Items.Single(u => u.Id == callerId);
        Assert.Equal("john@example.com", callerEntry.Email);
        Assert.Equal("john@example.com", callerEntry.Username);
        Assert.Equal("+420123456789", callerEntry.PhoneNumber);

        // Other user should be masked
        var otherEntry = masked.Items.Single(u => u.Id == otherId);
        Assert.Equal("o***@t***.org", otherEntry.Email);
        Assert.Equal("o***@t***.org", otherEntry.Username);
        Assert.Equal("***", otherEntry.PhoneNumber);
    }

    [Fact]
    public void WithMaskedPii_List_ShouldPreservePagination()
    {
        var response = new ListUsersResponse
        {
            Items = [CreateUser(Guid.NewGuid())],
            TotalCount = 50,
            PageNumber = 3,
            PageSize = 10
        };

        var masked = response.WithMaskedPii(Guid.NewGuid());

        Assert.Equal(50, masked.TotalCount);
        Assert.Equal(3, masked.PageNumber);
        Assert.Equal(10, masked.PageSize);
    }
}
