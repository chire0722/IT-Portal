using System.Buffers.Binary;

namespace IT_Form.Helpers;

/// <summary>
/// Validates browser-generated PNG data URLs before storing or rendering signatures.
/// </summary>
public static class SignatureDataUriValidator
{
    private const string Prefix = "data:image/png;base64,";
    private const int MaxEncodedLength = 500_000;
    private const int MaxImageBytes = 375_000;
    private const uint MaxDimension = 4096;
    private const ulong MaxPixelCount = 16_000_000;
    private static ReadOnlySpan<byte> PngSignature => [137, 80, 78, 71, 13, 10, 26, 10];

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > MaxEncodedLength
            || !value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var encoded = value.AsSpan(Prefix.Length);
        if (encoded.IsEmpty || encoded.Length > ((MaxImageBytes + 2) / 3) * 4)
        {
            return false;
        }

        var buffer = new byte[MaxImageBytes];
        if (!Convert.TryFromBase64String(encoded, buffer, out var bytesWritten)
            || bytesWritten < 24
            || !PngSignature.SequenceEqual(buffer.AsSpan(0, 8)))
        {
            return false;
        }

        // Require a canonical PNG IHDR as the first chunk and bound dimensions
        // before any downstream image/PDF library attempts to decode it.
        var ihdrLength = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(8, 4));
        if (ihdrLength != 13
            || !buffer.AsSpan(12, 4).SequenceEqual("IHDR"u8))
        {
            return false;
        }

        var width = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(20, 4));
        return width > 0
            && height > 0
            && width <= MaxDimension
            && height <= MaxDimension
            && (ulong)width * height <= MaxPixelCount;
    }
}
