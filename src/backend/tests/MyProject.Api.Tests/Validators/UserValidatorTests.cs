using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using MyProject.WebApi.Features.Authentication.Dtos.Login;
using MyProject.WebApi.Features.Users.Dtos;
using MyProject.WebApi.Features.Users.Dtos.DeleteAccount;
using MyProject.WebApi.Features.Users.Dtos.UploadAvatar;

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
            Bio = "Hello"
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
}

public class UploadAvatarRequestValidatorTests
{
    private readonly UploadAvatarRequestValidator _validator = new();

    [Fact]
    public void NullFile_ShouldFail() =>
        _validator.TestValidate(new UploadAvatarRequest())
            .ShouldHaveValidationErrorFor(x => x.File);

    [Fact]
    public void EmptyFile_ShouldFail()
    {
        var file = Substitute.For<IFormFile>();
        file.Length.Returns(0);
        file.ContentType.Returns("image/jpeg");

        _validator.TestValidate(new UploadAvatarRequest { File = file })
            .ShouldHaveValidationErrorFor(x => x.File.Length);
    }

    [Fact]
    public void FileTooLarge_ShouldFail()
    {
        var file = Substitute.For<IFormFile>();
        file.Length.Returns(6 * 1024 * 1024); // 6 MB
        file.ContentType.Returns("image/jpeg");

        _validator.TestValidate(new UploadAvatarRequest { File = file })
            .ShouldHaveValidationErrorFor(x => x.File.Length);
    }

    [Fact]
    public void InvalidContentType_ShouldFail()
    {
        var file = Substitute.For<IFormFile>();
        file.Length.Returns(1024);
        file.ContentType.Returns("application/pdf");

        _validator.TestValidate(new UploadAvatarRequest { File = file })
            .ShouldHaveValidationErrorFor(x => x.File.ContentType);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public void ValidFile_ShouldPass(string contentType)
    {
        var file = Substitute.For<IFormFile>();
        file.Length.Returns(1024);
        file.ContentType.Returns(contentType);

        _validator.TestValidate(new UploadAvatarRequest { File = file })
            .ShouldNotHaveAnyValidationErrors();
    }
}
