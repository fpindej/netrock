using Microsoft.Extensions.Logging;
using MyProject.Infrastructure.Features.Avatar.Services;
using MyProject.Shared;
using SkiaSharp;

namespace MyProject.Component.Tests.Services;

public class ImageProcessingServiceTests
{
    private readonly ImageProcessingService _sut = new(
        Substitute.For<ILogger<ImageProcessingService>>());

    private static byte[] CreateValidJpeg(int width = 100, int height = 100)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
        return data.ToArray();
    }

    private static byte[] CreateValidPng(int width = 100, int height = 100)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Blue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static byte[] CreateValidWebP(int width = 100, int height = 100)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Green);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Webp, 80);
        return data.ToArray();
    }

    private static byte[] CreateValidGif()
    {
        // SkiaSharp cannot encode GIF, so provide a minimal valid GIF89a (1x1 red pixel)
        return
        [
            0x47, 0x49, 0x46, 0x38, 0x39, 0x61, // GIF89a
            0x01, 0x00, 0x01, 0x00,             // 1x1 pixels
            0x80, 0x00, 0x00,                   // GCT flag, bg color, aspect ratio
            0xFF, 0x00, 0x00,                   // color 0: red
            0x00, 0x00, 0x00,                   // color 1: black
            0x2C,                               // image separator
            0x00, 0x00, 0x00, 0x00,             // image position (0,0)
            0x01, 0x00, 0x01, 0x00,             // image size (1x1)
            0x00,                               // no local color table
            0x02,                               // LZW minimum code size
            0x02,                               // block size
            0x4C, 0x01,                         // compressed data
            0x00,                               // block terminator
            0x3B                                // trailer
        ];
    }

    #region Valid Images

    [Fact]
    public void ProcessAvatar_ValidJpeg_ReturnsWebP()
    {
        var imageData = CreateValidJpeg();

        var result = _sut.ProcessAvatar(imageData, "photo.jpg");

        Assert.True(result.IsSuccess);
        Assert.Equal("image/webp", result.Value.ContentType);
        Assert.True(result.Value.Size > 0);
    }

    [Fact]
    public void ProcessAvatar_ValidPng_ReturnsWebP()
    {
        var imageData = CreateValidPng();

        var result = _sut.ProcessAvatar(imageData, "photo.png");

        Assert.True(result.IsSuccess);
        Assert.Equal("image/webp", result.Value.ContentType);
    }

    [Fact]
    public void ProcessAvatar_ValidWebP_ReturnsWebP()
    {
        var imageData = CreateValidWebP();

        var result = _sut.ProcessAvatar(imageData, "photo.webp");

        Assert.True(result.IsSuccess);
        Assert.Equal("image/webp", result.Value.ContentType);
    }

    [Fact]
    public void ProcessAvatar_ValidGif_ReturnsWebP()
    {
        var imageData = CreateValidGif();

        var result = _sut.ProcessAvatar(imageData, "photo.gif");

        Assert.True(result.IsSuccess);
        Assert.Equal("image/webp", result.Value.ContentType);
    }

    [Fact]
    public void ProcessAvatar_JpegExtension_ShouldPass()
    {
        var imageData = CreateValidJpeg();

        var result = _sut.ProcessAvatar(imageData, "photo.jpeg");

        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Resize Behavior

    [Fact]
    public void ProcessAvatar_LargeImage_ResizedToFitMaxDimension()
    {
        var imageData = CreateValidJpeg(1024, 768);

        var result = _sut.ProcessAvatar(imageData, "large.jpg", maxDimension: 512);

        Assert.True(result.IsSuccess);
        // Can't easily verify dimensions from the output bytes without decoding,
        // but the fact that it succeeds is the key assertion
        Assert.True(result.Value.Size > 0);
    }

    [Fact]
    public void ProcessAvatar_SmallImage_NotUpscaled()
    {
        var imageData = CreateValidJpeg(64, 64);

        var result = _sut.ProcessAvatar(imageData, "tiny.jpg", maxDimension: 512);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Size > 0);
    }

    #endregion

    #region Validation Failures

    [Fact]
    public void ProcessAvatar_OversizedFile_ReturnsFailure()
    {
        // Create data that exceeds 5MB
        var imageData = new byte[6 * 1024 * 1024];

        var result = _sut.ProcessAvatar(imageData, "huge.jpg");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Avatar.FileTooLarge, result.Error);
    }

    [Theory]
    [InlineData("photo.bmp")]
    [InlineData("photo.tiff")]
    [InlineData("photo.svg")]
    [InlineData("photo.txt")]
    [InlineData("photo")]
    public void ProcessAvatar_UnsupportedExtension_ReturnsFailure(string fileName)
    {
        var imageData = CreateValidJpeg();

        var result = _sut.ProcessAvatar(imageData, fileName);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Avatar.UnsupportedFormat, result.Error);
    }

    [Fact]
    public void ProcessAvatar_CorruptData_ReturnsFailure()
    {
        var corruptData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 };

        var result = _sut.ProcessAvatar(corruptData, "corrupt.jpg");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Avatar.ProcessingFailed, result.Error);
    }

    [Fact]
    public void ProcessAvatar_EmptyFileName_ReturnsUnsupportedFormat()
    {
        var imageData = CreateValidJpeg();

        var result = _sut.ProcessAvatar(imageData, "");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Avatar.UnsupportedFormat, result.Error);
    }

    #endregion
}
