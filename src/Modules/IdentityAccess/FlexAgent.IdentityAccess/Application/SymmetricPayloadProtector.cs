using System.Security.Cryptography;
using System.Text;

namespace FlexAgent.IdentityAccess.Application;

public interface ISymmetricPayloadProtector
{
    byte[] Protect(string plaintext);

    string Unprotect(byte[] ciphertext);
}

public sealed class AesGcmPayloadProtector(byte[] key) : ISymmetricPayloadProtector
{
    public byte[] Protect(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];
        nonce.CopyTo(payload, 0);
        tag.CopyTo(payload, nonce.Length);
        ciphertext.CopyTo(payload, nonce.Length + tag.Length);
        return payload;
    }

    public string Unprotect(byte[] ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        if (ciphertext.Length < 28)
        {
            throw new CryptographicException("Ciphertext is too short.");
        }

        var nonce = ciphertext.AsSpan(0, 12);
        var tag = ciphertext.AsSpan(12, 16);
        var encrypted = ciphertext.AsSpan(28);
        var plaintext = new byte[encrypted.Length];
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, encrypted, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
