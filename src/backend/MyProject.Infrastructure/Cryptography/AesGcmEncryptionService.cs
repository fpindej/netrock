using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MyProject.Application.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Options;

namespace MyProject.Infrastructure.Cryptography;

/// <summary>
/// AES-256-GCM implementation of <see cref="ISecretEncryptionService"/>.
/// Ciphertext format: <c>base64(nonce[12] || ciphertext[N] || tag[16])</c>.
/// The master key is derived via HKDF-SHA256 from the configured encryption key to ensure exactly 32 bytes.
/// </summary>
internal sealed class AesGcmEncryptionService : ISecretEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    private readonly byte[] _key;

    /// <summary>
    /// Initializes a new instance using the encryption key from <see cref="ExternalAuthOptions"/>.
    /// </summary>
    /// <param name="options">External auth configuration containing the master encryption key.</param>
    public AesGcmEncryptionService(IOptions<ExternalAuthOptions> options)
    {
        var ikm = Encoding.UTF8.GetBytes(options.Value.EncryptionKey);
        _key = HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, KeySize,
            info: "oauth-provider-encryption"u8.ToArray());
    }

    /// <inheritdoc />
    public string Encrypt(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[NonceSize + ciphertext.Length + TagSize];
        nonce.CopyTo(result, 0);
        ciphertext.CopyTo(result, NonceSize);
        tag.CopyTo(result, NonceSize + ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    /// <inheritdoc />
    public string Decrypt(string ciphertext)
    {
        var blob = Convert.FromBase64String(ciphertext);

        var nonce = blob.AsSpan(0, NonceSize);
        var tag = blob.AsSpan(blob.Length - TagSize);
        var encrypted = blob.AsSpan(NonceSize, blob.Length - NonceSize - TagSize);
        var plaintext = new byte[encrypted.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, encrypted, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }
}
