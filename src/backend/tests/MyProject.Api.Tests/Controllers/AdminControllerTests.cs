using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyProject.Api.Tests.Contracts;
using MyProject.Api.Tests.Fixtures;
using MyProject.Application.Features.Admin.Dtos;
using MyProject.Application.Features.Audit.Dtos;
using MyProject.Application.Identity.Constants;
using MyProject.Shared;

namespace MyProject.Api.Tests.Controllers;

public class AdminControllerTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetMocks();
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    private static HttpRequestMessage Get(string url, string auth)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Authorization", auth);
        return request;
    }

    private static HttpRequestMessage Post(string url, string auth, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.TryAddWithoutValidation("Authorization", auth);
        return request;
    }

    private static HttpRequestMessage Put(string url, string auth, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
        request.Headers.TryAddWithoutValidation("Authorization", auth);
        return request;
    }

    private static HttpRequestMessage Delete(string url, string auth)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.TryAddWithoutValidation("Authorization", auth);
        return request;
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response, int expectedStatus, string? expectedDetail = null)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(expectedStatus, json.GetProperty("status").GetInt32());
        if (expectedDetail is not null)
        {
            Assert.Equal(expectedDetail, json.GetProperty("detail").GetString());
        }
    }

    #region ListUsers

    [Fact]
    public async Task ListUsers_WithPermission_Returns200()
    {
        _factory.AdminService.GetUsersAsync(1, 10, null, Arg.Any<CancellationToken>())
            .Returns(new AdminUserListOutput([], 0, 1, 10));

        var response = await _client.SendAsync(
            Get("/api/v1/admin/users?pageNumber=1&pageSize=10", TestAuth.WithPermissions(AppPermissions.Users.View)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserListResponse>();
        Assert.NotNull(body);
        Assert.Equal(0, body.TotalCount);
        Assert.Equal(1, body.PageNumber);
        Assert.Equal(10, body.PageSize);
        Assert.NotNull(body.Items);
    }

    [Fact]
    public async Task ListUsers_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Get("/api/v1/admin/users?pageNumber=1&pageSize=10", TestAuth.User()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListUsers_Unauthenticated_Returns401()
    {
        using var anonClient = _factory.CreateClient();

        var response = await anonClient.GetAsync("/api/v1/admin/users?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListUsers_SuperAdmin_Returns200()
    {
        _factory.AdminService.GetUsersAsync(1, 10, null, Arg.Any<CancellationToken>())
            .Returns(new AdminUserListOutput([], 0, 1, 10));

        var response = await _client.SendAsync(
            Get("/api/v1/admin/users?pageNumber=1&pageSize=10", TestAuth.SuperAdmin()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserListResponse>();
        Assert.NotNull(body);
        Assert.Equal(0, body.TotalCount);
        Assert.Equal(1, body.PageNumber);
        Assert.Equal(10, body.PageSize);
        Assert.NotNull(body.Items);
    }

    #endregion

    #region GetUser

    [Fact]
    public async Task GetUser_WithPermission_Returns200()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.GetUserByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<AdminUserOutput>.Success(new AdminUserOutput(
                userId, "user@test.com", "John", "Doe", null, null, false,
                ["User"], true, true, null, 0, false)));

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/users/{userId}", TestAuth.WithPermissions(AppPermissions.Users.View, AppPermissions.Users.ViewPii)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserResponse>();
        Assert.NotNull(body);
        Assert.Equal(userId, body.Id);
        Assert.Equal("user@test.com", body.Username);
        Assert.Contains("User", body.Roles);
    }

    [Fact]
    public async Task GetUser_NotFound_Returns404WithProblemDetails()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.GetUserByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<AdminUserOutput>.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound));

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/users/{userId}", TestAuth.WithPermissions(AppPermissions.Users.View)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetailsAsync(response, 404, ErrorMessages.Admin.UserNotFound);
    }

    [Fact]
    public async Task GetUser_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Get($"/api/v1/admin/users/{Guid.NewGuid()}", TestAuth.User()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region PII Masking

    [Fact]
    public async Task ListUsers_ViewOnly_MasksPiiExceptSelf()
    {
        var callerId = Guid.Parse(TestAuthHandler.DefaultUserId);
        var otherId = Guid.NewGuid();
        _factory.AdminService.GetUsersAsync(1, 10, null, Arg.Any<CancellationToken>())
            .Returns(new AdminUserListOutput(
            [
                new AdminUserOutput(callerId, "caller@test.com", "Alice", "A", "+420111222333", null, false, ["User"], true, true, null, 0, false),
                new AdminUserOutput(otherId, "other@test.com", "Bob", "B", "+420999888777", null, false, ["User"], true, true, null, 0, false)
            ], 2, 1, 10));

        var response = await _client.SendAsync(
            Get("/api/v1/admin/users?pageNumber=1&pageSize=10", TestAuth.WithPermissions(AppPermissions.Users.View)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserListResponse>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);

        var callerEntry = body.Items.Single(u => u.Id == callerId);
        Assert.Equal("caller@test.com", callerEntry.Email);
        Assert.Equal("+420111222333", callerEntry.PhoneNumber);

        var otherEntry = body.Items.Single(u => u.Id == otherId);
        Assert.NotEqual("other@test.com", otherEntry.Email);
        Assert.Contains("***", otherEntry.Email);
        Assert.Equal("***", otherEntry.PhoneNumber);
    }

    [Fact]
    public async Task GetUser_ViewOnly_OtherUser_ReturnsMaskedPii()
    {
        var otherId = Guid.NewGuid();
        _factory.AdminService.GetUserByIdAsync(otherId, Arg.Any<CancellationToken>())
            .Returns(Result<AdminUserOutput>.Success(new AdminUserOutput(
                otherId, "other@test.com", "Bob", "B", "+420999888777", null, false,
                ["User"], true, true, null, 0, false)));

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/users/{otherId}", TestAuth.WithPermissions(AppPermissions.Users.View)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserResponse>();
        Assert.NotNull(body);
        Assert.NotEqual("other@test.com", body.Email);
        Assert.Contains("***", body.Email);
        Assert.NotEqual("other@test.com", body.Username);
        Assert.Equal("***", body.PhoneNumber);
        // Non-PII fields are preserved
        Assert.Equal("Bob", body.FirstName);
        Assert.Equal("B", body.LastName);
    }

    [Fact]
    public async Task GetUser_SelfView_WithoutViewPii_ReturnsUnmaskedPii()
    {
        var callerId = Guid.Parse(TestAuthHandler.DefaultUserId);
        _factory.AdminService.GetUserByIdAsync(callerId, Arg.Any<CancellationToken>())
            .Returns(Result<AdminUserOutput>.Success(new AdminUserOutput(
                callerId, "caller@test.com", "Alice", "A", "+420111222333", null, false,
                ["User"], true, true, null, 0, false)));

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/users/{callerId}", TestAuth.WithPermissions(AppPermissions.Users.View)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserResponse>();
        Assert.NotNull(body);
        Assert.Equal("caller@test.com", body.Email);
        Assert.Equal("caller@test.com", body.Username);
        Assert.Equal("+420111222333", body.PhoneNumber);
    }

    [Fact]
    public async Task ListUsers_SuperAdmin_ReturnsUnmaskedPii()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.GetUsersAsync(1, 10, null, Arg.Any<CancellationToken>())
            .Returns(new AdminUserListOutput(
            [
                new AdminUserOutput(userId, "user@test.com", "John", "D", "+420123456789", null, false, ["User"], true, true, null, 0, false)
            ], 1, 1, 10));

        var response = await _client.SendAsync(
            Get("/api/v1/admin/users?pageNumber=1&pageSize=10", TestAuth.SuperAdmin()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserListResponse>();
        Assert.NotNull(body);
        Assert.Single(body.Items);
        Assert.Equal("user@test.com", body.Items[0].Email);
        Assert.Equal("+420123456789", body.Items[0].PhoneNumber);
    }

    [Fact]
    public async Task GetUser_SuperAdmin_ReturnsUnmaskedPii()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.GetUserByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result<AdminUserOutput>.Success(new AdminUserOutput(
                userId, "user@test.com", "John", "D", "+420123456789", null, false,
                ["User"], true, true, null, 0, false)));

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/users/{userId}", TestAuth.SuperAdmin()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserResponse>();
        Assert.NotNull(body);
        Assert.Equal("user@test.com", body.Email);
        Assert.Equal("user@test.com", body.Username);
        Assert.Equal("+420123456789", body.PhoneNumber);
    }

    #endregion

    #region AssignRole

    [Fact]
    public async Task AssignRole_WithPermission_Returns204()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.AssignRoleAsync(
                Arg.Any<Guid>(), userId, Arg.Any<AssignRoleInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/roles",
                TestAuth.WithPermissions(AppPermissions.Users.AssignRoles),
                JsonContent.Create(new { Role = "User" })));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AssignRole_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{Guid.NewGuid()}/roles",
                TestAuth.User(),
                JsonContent.Create(new { Role = "User" })));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AssignRole_ServiceFailure_Returns400WithProblemDetails()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.AssignRoleAsync(
                Arg.Any<Guid>(), userId, Arg.Any<AssignRoleInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.Admin.RoleAssignAboveRank));

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/roles",
                TestAuth.WithPermissions(AppPermissions.Users.AssignRoles),
                JsonContent.Create(new { Role = "Admin" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.Admin.RoleAssignAboveRank);
    }

    [Fact]
    public async Task AssignRole_EmailNotVerified_Returns400()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.AssignRoleAsync(
                Arg.Any<Guid>(), userId, Arg.Any<AssignRoleInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.Admin.EmailVerificationRequired));

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/roles",
                TestAuth.WithPermissions(AppPermissions.Users.AssignRoles),
                JsonContent.Create(new { Role = "User" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.Admin.EmailVerificationRequired);
    }

    #endregion

    #region RemoveRole

    [Fact]
    public async Task RemoveRole_WithPermission_Returns204()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.RemoveRoleAsync(
                Arg.Any<Guid>(), userId, "User", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Delete($"/api/v1/admin/users/{userId}/roles/User",
                TestAuth.WithPermissions(AppPermissions.Users.AssignRoles)));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RemoveRole_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Delete($"/api/v1/admin/users/{Guid.NewGuid()}/roles/User", TestAuth.User()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region LockUser

    [Fact]
    public async Task LockUser_WithPermission_Returns204()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.LockUserAsync(Arg.Any<Guid>(), userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/lock", TestAuth.WithPermissions(AppPermissions.Users.Manage)));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task LockUser_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{Guid.NewGuid()}/lock", TestAuth.User()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region UnlockUser

    [Fact]
    public async Task UnlockUser_WithPermission_Returns204()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.UnlockUserAsync(Arg.Any<Guid>(), userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/unlock", TestAuth.WithPermissions(AppPermissions.Users.Manage)));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    #endregion

    #region DeleteUser

    [Fact]
    public async Task DeleteUser_WithPermission_Returns204()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.DeleteUserAsync(Arg.Any<Guid>(), userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Delete($"/api/v1/admin/users/{userId}", TestAuth.WithPermissions(AppPermissions.Users.Manage)));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_NotFound_Returns404WithProblemDetails()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.DeleteUserAsync(Arg.Any<Guid>(), userId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound));

        var response = await _client.SendAsync(
            Delete($"/api/v1/admin/users/{userId}", TestAuth.WithPermissions(AppPermissions.Users.Manage)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetailsAsync(response, 404, ErrorMessages.Admin.UserNotFound);
    }

    [Fact]
    public async Task DeleteUser_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Delete($"/api/v1/admin/users/{Guid.NewGuid()}", TestAuth.User()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region VerifyEmail

    [Fact]
    public async Task VerifyEmail_WithPermission_Returns204()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.VerifyEmailAsync(Arg.Any<Guid>(), userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/verify-email", TestAuth.WithPermissions(AppPermissions.Users.Manage)));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{Guid.NewGuid()}/verify-email", TestAuth.User()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VerifyEmail_NotFound_Returns404()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.VerifyEmailAsync(Arg.Any<Guid>(), userId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound));

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/verify-email", TestAuth.WithPermissions(AppPermissions.Users.Manage)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetailsAsync(response, 404, ErrorMessages.Admin.UserNotFound);
    }

    [Fact]
    public async Task VerifyEmail_AlreadyVerified_Returns400()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.VerifyEmailAsync(Arg.Any<Guid>(), userId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.Auth.EmailAlreadyVerified));

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/verify-email", TestAuth.WithPermissions(AppPermissions.Users.Manage)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.Auth.EmailAlreadyVerified);
    }

    #endregion

    #region SendPasswordReset

    [Fact]
    public async Task SendPasswordReset_WithPermission_Returns204()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.SendPasswordResetAsync(Arg.Any<Guid>(), userId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/send-password-reset", TestAuth.WithPermissions(AppPermissions.Users.Manage)));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SendPasswordReset_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{Guid.NewGuid()}/send-password-reset", TestAuth.User()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendPasswordReset_NotFound_Returns404()
    {
        var userId = Guid.NewGuid();
        _factory.AdminService.SendPasswordResetAsync(Arg.Any<Guid>(), userId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound));

        var response = await _client.SendAsync(
            Post($"/api/v1/admin/users/{userId}/send-password-reset", TestAuth.WithPermissions(AppPermissions.Users.Manage)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetailsAsync(response, 404, ErrorMessages.Admin.UserNotFound);
    }

    #endregion

    #region CreateUser

    [Fact]
    public async Task CreateUser_WithPermission_Returns201()
    {
        _factory.AdminService.CreateUserAsync(Arg.Any<Guid>(), Arg.Any<CreateUserInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        var response = await _client.SendAsync(
            Post("/api/v1/admin/users",
                TestAuth.WithPermissions(AppPermissions.Users.Manage),
                JsonContent.Create(new { Email = "new@test.com", FirstName = "John", LastName = "Doe" })));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task CreateUser_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Post("/api/v1/admin/users",
                TestAuth.User(),
                JsonContent.Create(new { Email = "new@test.com" })));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Returns400()
    {
        _factory.AdminService.CreateUserAsync(Arg.Any<Guid>(), Arg.Any<CreateUserInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure(ErrorMessages.Admin.EmailAlreadyRegistered));

        var response = await _client.SendAsync(
            Post("/api/v1/admin/users",
                TestAuth.WithPermissions(AppPermissions.Users.Manage),
                JsonContent.Create(new { Email = "existing@test.com" })));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertProblemDetailsAsync(response, 400, ErrorMessages.Admin.EmailAlreadyRegistered);
    }

    #endregion

    #region Roles CRUD

    [Fact]
    public async Task ListRoles_WithPermission_Returns200()
    {
        _factory.AdminService.GetRolesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AdminRoleOutput>
            {
                new(Guid.NewGuid(), "Admin", "Administrator role", true, 3, ["users.view", "roles.manage"])
            });

        var response = await _client.SendAsync(
            Get("/api/v1/admin/roles", TestAuth.WithPermissions(AppPermissions.Roles.View)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<AdminRoleResponse>>();
        Assert.NotNull(body);
        Assert.Single(body);
        Assert.Equal("Admin", body[0].Name);
        Assert.NotEqual(Guid.Empty, body[0].Id);
    }

    [Fact]
    public async Task ListRoles_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Get("/api/v1/admin/roles", TestAuth.User()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetRole_WithPermission_Returns200()
    {
        var roleId = Guid.NewGuid();
        _factory.RoleManagementService.GetRoleDetailAsync(roleId, Arg.Any<CancellationToken>())
            .Returns(Result<RoleDetailOutput>.Success(
                new RoleDetailOutput(roleId, "Admin", "Admin role", true, [], 5)));

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/roles/{roleId}", TestAuth.WithPermissions(AppPermissions.Roles.View)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RoleDetailResponse>();
        Assert.NotNull(body);
        Assert.Equal(roleId, body.Id);
        Assert.Equal("Admin", body.Name);
        Assert.NotNull(body.Permissions);
    }

    [Fact]
    public async Task GetRole_NotFound_Returns404WithProblemDetails()
    {
        var roleId = Guid.NewGuid();
        _factory.RoleManagementService.GetRoleDetailAsync(roleId, Arg.Any<CancellationToken>())
            .Returns(Result<RoleDetailOutput>.Failure(ErrorMessages.Roles.RoleNotFound, ErrorType.NotFound));

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/roles/{roleId}", TestAuth.WithPermissions(AppPermissions.Roles.View)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertProblemDetailsAsync(response, 404, ErrorMessages.Roles.RoleNotFound);
    }

    [Fact]
    public async Task CreateRole_WithPermission_Returns201()
    {
        _factory.RoleManagementService.CreateRoleAsync(Arg.Any<CreateRoleInput>(), Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(Guid.NewGuid()));

        var response = await _client.SendAsync(
            Post("/api/v1/admin/roles",
                TestAuth.WithPermissions(AppPermissions.Roles.Manage),
                JsonContent.Create(new { Name = "CustomRole", Description = "A custom role" })));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateRoleResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    public async Task CreateRole_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Post("/api/v1/admin/roles",
                TestAuth.WithPermissions(AppPermissions.Roles.View),
                JsonContent.Create(new { Name = "CustomRole" })));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRole_WithPermission_Returns204()
    {
        var roleId = Guid.NewGuid();
        _factory.RoleManagementService.UpdateRoleAsync(roleId, Arg.Any<UpdateRoleInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Put($"/api/v1/admin/roles/{roleId}",
                TestAuth.WithPermissions(AppPermissions.Roles.Manage),
                JsonContent.Create(new { Name = "NewName" })));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRole_WithPermission_Returns204()
    {
        var roleId = Guid.NewGuid();
        _factory.RoleManagementService.DeleteRoleAsync(roleId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Delete($"/api/v1/admin/roles/{roleId}", TestAuth.WithPermissions(AppPermissions.Roles.Manage)));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    #endregion

    #region Permissions

    [Fact]
    public async Task SetPermissions_WithPermission_Returns204()
    {
        var roleId = Guid.NewGuid();
        _factory.RoleManagementService.SetRolePermissionsAsync(
                roleId, Arg.Any<SetRolePermissionsInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var response = await _client.SendAsync(
            Put($"/api/v1/admin/roles/{roleId}/permissions",
                TestAuth.WithPermissions(AppPermissions.Roles.Manage),
                JsonContent.Create(new { Permissions = new[] { "users.view" } })));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetAllPermissions_WithPermission_Returns200()
    {
        _factory.RoleManagementService.GetAllPermissions()
            .Returns(new List<PermissionGroupOutput>
            {
                new("Users", ["users.view", "users.manage"])
            });

        var response = await _client.SendAsync(
            Get("/api/v1/admin/permissions", TestAuth.WithPermissions(AppPermissions.Roles.View)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<PermissionGroupResponse>>();
        Assert.NotNull(body);
        Assert.Single(body);
        Assert.Equal("Users", body[0].Category);
        Assert.NotNull(body[0].Permissions);
        Assert.Contains("users.view", body[0].Permissions);
    }

    [Fact]
    public async Task GetAllPermissions_WithoutPermission_Returns403()
    {
        var response = await _client.SendAsync(
            Get("/api/v1/admin/permissions", TestAuth.User()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region GetUserAuditTrail

    [Fact]
    public async Task GetUserAuditTrail_WithPermission_Returns200()
    {
        var userId = Guid.NewGuid();
        _factory.AuditService.GetUserAuditEventsAsync(
                userId, 1, 10, Arg.Any<CancellationToken>())
            .Returns(new AuditEventListOutput([], 0, 1, 10));

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/users/{userId}/audit?pageNumber=1&pageSize=10",
                TestAuth.WithPermissions(AppPermissions.Users.View)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListAuditEventsContract>();
        Assert.NotNull(body);
        Assert.Equal(0, body.TotalCount);
        Assert.Equal(1, body.PageNumber);
        Assert.Equal(10, body.PageSize);
    }

    [Fact]
    public async Task GetUserAuditTrail_WithoutPermission_Returns403()
    {
        var userId = Guid.NewGuid();

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/users/{userId}/audit?pageNumber=1&pageSize=10", TestAuth.User()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserAuditTrail_Unauthenticated_Returns401()
    {
        var userId = Guid.NewGuid();
        using var anonClient = _factory.CreateClient();

        var response = await anonClient.GetAsync($"/api/v1/admin/users/{userId}/audit");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserAuditTrail_SuperAdmin_Returns200()
    {
        var userId = Guid.NewGuid();
        _factory.AuditService.GetUserAuditEventsAsync(
                userId, 1, 10, Arg.Any<CancellationToken>())
            .Returns(new AuditEventListOutput([], 0, 1, 10));

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/users/{userId}/audit?pageNumber=1&pageSize=10", TestAuth.SuperAdmin()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserAuditTrail_WithEvents_ReturnsItems()
    {
        var userId = Guid.NewGuid();
        var events = new List<AuditEventOutput>
        {
            new(Guid.NewGuid(), userId, "LoginSuccess", null, null, null, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), "AdminLockUser", "User", userId, null, DateTime.UtcNow)
        };
        _factory.AuditService.GetUserAuditEventsAsync(
                userId, 1, 10, Arg.Any<CancellationToken>())
            .Returns(new AuditEventListOutput(events, 2, 1, 10));

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/users/{userId}/audit?pageNumber=1&pageSize=10",
                TestAuth.WithPermissions(AppPermissions.Users.View)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ListAuditEventsContract>();
        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(2, body.TotalCount);
    }

    #endregion
}
