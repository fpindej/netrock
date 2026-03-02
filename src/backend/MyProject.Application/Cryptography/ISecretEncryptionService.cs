namespace MyProject.Application.Cryptography;

/// <summary>
/// Encrypts and decrypts secrets at rest (e.g. OAuth client credentials).
/// The default implementation uses AES-256-GCM with a master key from configuration.
/// </summary>
public interface ISecretEncryptionService
{
    /// <summary>
    /// Encrypts <paramref name="plaintext"/> and returns a base64 string
    /// containing <c>nonce || ciphertext || tag</c>.
    /// </summary>
    /// <param name="plaintext">The value to encrypt.</param>
    /// <returns>A base64-encoded ciphertext blob.</returns>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts a value previously produced by <see cref="Encrypt"/>.
    /// </summary>
    /// <param name="ciphertext">The base64-encoded ciphertext blob.</param>
    /// <returns>The original plaintext.</returns>
    string Decrypt(string ciphertext);
}
