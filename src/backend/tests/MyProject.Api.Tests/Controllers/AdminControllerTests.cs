using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyProject.Api.Tests.Contracts;
using MyProject.Api.Tests.Fixtures;
using MyProject.Application.Features.Admin.Dtos;
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

    private HttpRequestMessage Get(string url, string auth) =>
        new(HttpMethod.Get, url) { Headers = { { "Authorization", auth } } };

    private HttpRequestMessage Post(string url, string auth, HttpContent? content = null) =>
        new(HttpMethod.Post, url) { Headers = { { "Authorization", auth } }, Content = content };

    private HttpRequestMessage Put(string url, string auth, HttpContent? content = null) =>
        new(HttpMethod.Put, url) { Headers = { { "Authorization", auth } }, Content = content };

    private HttpRequestMessage Delete(string url, string auth) =>
        new(HttpMethod.Delete, url) { Headers = { { "Authorization", auth } } };

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
                userId, "user@test.com", "John", "Doe", null, null, null,
                ["User"], true, true, null, 0, false)));

        var response = await _client.SendAsync(
            Get($"/api/v1/admin/users/{userId}", TestAuth.WithPermissions(AppPermissions.Users.View)));

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

    #region Roles CRUD

    [Fact]
    public async Task ListRoles_WithPermission_Returns200()
    {
        _factory.AdminService.GetRolesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AdminRoleOutput>
            {
                new(Guid.NewGuid(), "Admin", "Administrator role", true, 3)
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
}
