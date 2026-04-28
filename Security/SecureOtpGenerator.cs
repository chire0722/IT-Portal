// Project comment: SecureOtpGenerator.cs is part of the IT Form application source code.

using System.Security.Cryptography;

namespace IT_Form.Security;

/// <summary>
/// Defines the SecureOtpGenerator class used by the IT Form application.
/// </summary>
public sealed class SecureOtpGenerator
{
    private const string AllowedCharacters = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    /// <summary>
    /// Runs the GenerateCode operation.
    /// </summary>
    public string GenerateCode(int length = 6)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

        Span<char> buffer = length <= 128
            ? stackalloc char[length]
            : new char[length];

        for (var index = 0; index < buffer.Length; index++)
        {
            buffer[index] = AllowedCharacters[RandomNumberGenerator.GetInt32(AllowedCharacters.Length)];
        }

        return new string(buffer);
    }
}
