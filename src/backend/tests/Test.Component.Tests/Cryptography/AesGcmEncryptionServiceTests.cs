using Microsoft.Extensions.Options;
using Test.Infrastructure.Cryptography;
using Test.Infrastructure.Features.Authentication.Options;

namespace Test.Component.Tests.Cryptography;

public class AesGcmEncryptionServiceTests
{
    private readonly AesGcmEncryptionService _sut;

    public AesGcmEncryptionServiceTests()
    {
        var options = Options.Create(new ExternalAuthOptions
        {
            EncryptionKey = "test-encryption-key-that-is-long-enough-for-hkdf-derivation",
            AllowedRedirectUris = []
        });
        _sut = new AesGcmEncryptionService(options);
    }

    [Fact]
    public void Encrypt_Decrypt_RoundTrips()
    {
        const string plaintext = "my-client-secret-value";

        var ciphertext = _sut.Encrypt(plaintext);
        var decrypted = _sut.Decrypt(ciphertext);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachTime()
    {
        const string plaintext = "same-value";

        var ciphertext1 = _sut.Encrypt(plaintext);
        var ciphertext2 = _sut.Encrypt(plaintext);

        Assert.NotEqual(ciphertext1, ciphertext2);
    }

    [Fact]
    public void Encrypt_Decrypt_HandlesEmptyString()
    {
        var ciphertext = _sut.Encrypt(string.Empty);
        var decrypted = _sut.Decrypt(ciphertext);

        Assert.Equal(string.Empty, decrypted);
    }

    [Fact]
    public void Encrypt_Decrypt_HandlesUnicodeCharacters()
    {
        const string plaintext = "secret-with-unicode-\u00e9\u00e8\u00ea";

        var ciphertext = _sut.Encrypt(plaintext);
        var decrypted = _sut.Decrypt(ciphertext);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Decrypt_WithTamperedCiphertext_Throws()
    {
        var ciphertext = _sut.Encrypt("original-value");
        var bytes = Convert.FromBase64String(ciphertext);
        bytes[15] ^= 0xFF; // Tamper with ciphertext portion
        var tampered = Convert.ToBase64String(bytes);

        Assert.ThrowsAny<Exception>(() => _sut.Decrypt(tampered));
    }

    [Fact]
    public void Decrypt_WithDifferentKey_Throws()
    {
        var ciphertext = _sut.Encrypt("secret-value");

        var otherOptions = Options.Create(new ExternalAuthOptions
        {
            EncryptionKey = "completely-different-key-for-testing-mismatch-scenario",
            AllowedRedirectUris = []
        });
        var otherService = new AesGcmEncryptionService(otherOptions);

        Assert.ThrowsAny<Exception>(() => otherService.Decrypt(ciphertext));
    }

    [Fact]
    public void Encrypt_OutputIsValidBase64()
    {
        var ciphertext = _sut.Encrypt("test-value");

        var bytes = Convert.FromBase64String(ciphertext);
        Assert.True(bytes.Length > 28); // At least nonce(12) + tag(16) + 1 byte ciphertext
    }

    [Fact]
    public void Decrypt_WithTruncatedBlob_ThrowsCryptographicException()
    {
        var shortBlob = Convert.ToBase64String(new byte[10]); // Less than nonce(12) + tag(16)

        Assert.Throws<System.Security.Cryptography.CryptographicException>(
            () => _sut.Decrypt(shortBlob));
    }
}
