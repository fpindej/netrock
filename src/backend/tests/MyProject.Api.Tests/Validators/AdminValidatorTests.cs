using FluentValidation.TestHelper;
using MyProject.WebApi.Features.Admin.Dtos.AssignRole;
using MyProject.WebApi.Features.Admin.Dtos.CreateRole;
using MyProject.WebApi.Features.Admin.Dtos.ListUsers;
using MyProject.WebApi.Features.Admin.Dtos.SetPermissions;
using MyProject.WebApi.Features.Admin.Dtos.UpdateRole;

namespace MyProject.Api.Tests.Validators;

public class AssignRoleRequestValidatorTests
{
    private readonly AssignRoleRequestValidator _validator = new();

    [Fact]
    public void ValidRole_ShouldPass() =>
        _validator.TestValidate(new AssignRoleRequest { Role = "Admin" }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyRole_ShouldFail() =>
        _validator.TestValidate(new AssignRoleRequest { Role = "" }).ShouldHaveValidationErrorFor(x => x.Role);

    [Fact]
    public void RoleTooLong_ShouldFail() =>
        _validator.TestValidate(new AssignRoleRequest { Role = new string('a', 51) })
            .ShouldHaveValidationErrorFor(x => x.Role);
}

public class CreateRoleRequestValidatorTests
{
    private readonly CreateRoleRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_ShouldPass() =>
        _validator.TestValidate(new CreateRoleRequest { Name = "CustomRole", Description = "Desc" })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyName_ShouldFail() =>
        _validator.TestValidate(new CreateRoleRequest { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void NameTooLong_ShouldFail() =>
        _validator.TestValidate(new CreateRoleRequest { Name = new string('a', 51) })
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Theory]
    [InlineData("1Invalid")]
    [InlineData("has space")]
    [InlineData("has.dot")]
    public void NameInvalidPattern_ShouldFail(string name) =>
        _validator.TestValidate(new CreateRoleRequest { Name = name }).ShouldHaveValidationErrorFor(x => x.Name);

    [Theory]
    [InlineData("ValidName")]
    [InlineData("Role-Name")]
    [InlineData("Role_Name")]
    [InlineData("Role123")]
    public void NameValidPattern_ShouldPass(string name) =>
        _validator.TestValidate(new CreateRoleRequest { Name = name }).ShouldNotHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void DescriptionTooLong_ShouldFail() =>
        _validator.TestValidate(new CreateRoleRequest { Name = "Valid", Description = new string('a', 201) })
            .ShouldHaveValidationErrorFor(x => x.Description);
}

public class UpdateRoleRequestValidatorTests
{
    private readonly UpdateRoleRequestValidator _validator = new();

    [Fact]
    public void BothFieldsNull_ShouldFail() =>
        _validator.TestValidate(new UpdateRoleRequest()).ShouldHaveAnyValidationError();

    [Fact]
    public void NameOnly_ShouldPass() =>
        _validator.TestValidate(new UpdateRoleRequest { Name = "Valid" }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void DescriptionOnly_ShouldPass() =>
        _validator.TestValidate(new UpdateRoleRequest { Description = "Updated" }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NameInvalidPattern_ShouldFail() =>
        _validator.TestValidate(new UpdateRoleRequest { Name = "1bad" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void NameTooLong_ShouldFail() =>
        _validator.TestValidate(new UpdateRoleRequest { Name = new string('a', 51) })
            .ShouldHaveValidationErrorFor(x => x.Name);
}

public class SetPermissionsRequestValidatorTests
{
    private readonly SetPermissionsRequestValidator _validator = new();

    [Fact]
    public void ValidPermissions_ShouldPass() =>
        _validator.TestValidate(new SetPermissionsRequest { Permissions = ["users.view", "roles.manage"] })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void TooManyPermissions_ShouldFail() =>
        _validator.TestValidate(new SetPermissionsRequest
            {
                Permissions = Enumerable.Range(0, 51).Select(i => $"perm.{i}").ToList()
            })
            .ShouldHaveValidationErrorFor(x => x.Permissions);

    [Theory]
    [InlineData("Invalid")]
    [InlineData("UPPERCASE")]
    [InlineData("has space")]
    public void InvalidPermissionPattern_ShouldFail(string permission) =>
        _validator.TestValidate(new SetPermissionsRequest { Permissions = [permission] })
            .ShouldHaveValidationErrorFor("Permissions[0]");

    [Theory]
    [InlineData("users.view")]
    [InlineData("roles.manage")]
    [InlineData("jobs_view")]
    public void ValidPermissionPattern_ShouldPass(string permission) =>
        _validator.TestValidate(new SetPermissionsRequest { Permissions = [permission] })
            .ShouldNotHaveAnyValidationErrors();
}

public class ListUsersRequestValidatorTests
{
    private readonly ListUsersRequestValidator _validator = new();

    [Fact]
    public void DefaultValues_ShouldPass() =>
        _validator.TestValidate(new ListUsersRequest()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void PageNumberZero_ShouldFail() =>
        _validator.TestValidate(new ListUsersRequest { PageNumber = 0 })
            .ShouldHaveValidationErrorFor(x => x.PageNumber);

    [Fact]
    public void PageSizeZero_ShouldFail() =>
        _validator.TestValidate(new ListUsersRequest { PageSize = 0 })
            .ShouldHaveValidationErrorFor(x => x.PageSize);

    [Fact]
    public void PageSizeTooLarge_ShouldFail() =>
        _validator.TestValidate(new ListUsersRequest { PageSize = 101 })
            .ShouldHaveValidationErrorFor(x => x.PageSize);

    [Fact]
    public void SearchTooLong_ShouldFail() =>
        _validator.TestValidate(new ListUsersRequest { Search = new string('a', 201) })
            .ShouldHaveValidationErrorFor(x => x.Search);
}
