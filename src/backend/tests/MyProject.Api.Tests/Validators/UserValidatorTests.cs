using FluentValidation.TestHelper;
using MyProject.WebApi.Features.Authentication.Dtos.Login;
using MyProject.WebApi.Features.Users.Dtos.DeleteAccount;
using MyProject.WebApi.Features.Users.Dtos;

namespace MyProject.Api.Tests.Validators;

public class RefreshRequestValidatorTests
{
    private readonly RefreshRequestValidator _validator = new();

    [Fact]
    public void NullToken_ShouldPass() =>
        _validator.TestValidate(new RefreshRequest()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ValidToken_ShouldPass() =>
        _validator.TestValidate(new RefreshRequest { RefreshToken = "abc123" }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void TokenTooLong_ShouldFail() =>
        _validator.TestValidate(new RefreshRequest { RefreshToken = new string('a', 501) })
            .ShouldHaveValidationErrorFor(x => x.RefreshToken);
}

public class DeleteAccountRequestValidatorTests
{
    private readonly DeleteAccountRequestValidator _validator = new();

    [Fact]
    public void ValidPassword_ShouldPass() =>
        _validator.TestValidate(new DeleteAccountRequest { Password = "MyPassword1!" })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void EmptyPassword_ShouldFail() =>
        _validator.TestValidate(new DeleteAccountRequest { Password = "" })
            .ShouldHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void PasswordTooLong_ShouldFail() =>
        _validator.TestValidate(new DeleteAccountRequest { Password = new string('a', 256) })
            .ShouldHaveValidationErrorFor(x => x.Password);
}

public class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserRequestValidator _validator = new();

    [Fact]
    public void EmptyRequest_ShouldPass() =>
        _validator.TestValidate(new UpdateUserRequest()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ValidFullRequest_ShouldPass() =>
        _validator.TestValidate(new UpdateUserRequest
        {
            FirstName = "Jane",
            LastName = "Doe",
            PhoneNumber = "+420123456789",
            Bio = "Hello",
            AvatarUrl = "https://example.com/avatar.jpg"
        }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void FirstNameTooLong_ShouldFail() =>
        _validator.TestValidate(new UpdateUserRequest { FirstName = new string('a', 256) })
            .ShouldHaveValidationErrorFor(x => x.FirstName);

    [Fact]
    public void LastNameTooLong_ShouldFail() =>
        _validator.TestValidate(new UpdateUserRequest { LastName = new string('a', 256) })
            .ShouldHaveValidationErrorFor(x => x.LastName);

    [Theory]
    [InlineData("+420123456789")]
    [InlineData("+1 1234567890")]
    [InlineData("123456789")]
    public void ValidPhoneNumber_ShouldPass(string phone) =>
        _validator.TestValidate(new UpdateUserRequest { PhoneNumber = phone })
            .ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);

    [Theory]
    [InlineData("abc")]
    [InlineData("++123")]
    [InlineData("12345")]
    public void InvalidPhoneNumber_ShouldFail(string phone) =>
        _validator.TestValidate(new UpdateUserRequest { PhoneNumber = phone })
            .ShouldHaveValidationErrorFor(x => x.PhoneNumber);

    [Fact]
    public void BioTooLong_ShouldFail() =>
        _validator.TestValidate(new UpdateUserRequest { Bio = new string('a', 1001) })
            .ShouldHaveValidationErrorFor(x => x.Bio);

    [Theory]
    [InlineData("https://example.com/avatar.jpg")]
    [InlineData("http://example.com/photo.png")]
    public void ValidAvatarUrl_ShouldPass(string url) =>
        _validator.TestValidate(new UpdateUserRequest { AvatarUrl = url })
            .ShouldNotHaveValidationErrorFor(x => x.AvatarUrl);

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("not-a-url")]
    public void InvalidAvatarUrl_ShouldFail(string url) =>
        _validator.TestValidate(new UpdateUserRequest { AvatarUrl = url })
            .ShouldHaveValidationErrorFor(x => x.AvatarUrl);
}
