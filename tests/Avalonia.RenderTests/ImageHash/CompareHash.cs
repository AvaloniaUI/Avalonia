using System;

namespace Avalonia.UnitTests;

/// <summary>
/// Utility to compare 64 bit hashes using the Hamming distance.
/// </summary>
public static class CompareHash
{
    /// <summary>
    /// Array used for BitCount method (used in Similarity comparisons).
    /// Array corresponds to the number of 1's per value.
    /// ie. index 0 => byte 0x00 = 0000 0000 -> 0 high bits
    /// ie. index 1 => byte 0x01 = 0000 0001 -> 1 high bit
    /// ie. index 3 => byte 0x03 = 0000 0011 -> 2 high bits
    /// ie. index 255 =>    0xFF = 1111 1111 -> 8 high bits
    /// etc.
    /// </summary>
    private static readonly byte[] bitCounts =
        {
                0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4, 1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5,
                1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
                1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
                2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
                1, 2, 2, 3, 2, 3, 3, 4, 2, 3, 3, 4, 3, 4, 4, 5, 2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6,
                2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
                2, 3, 3, 4, 3, 4, 4, 5, 3, 4, 4, 5, 4, 5, 5, 6, 3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7,
                3, 4, 4, 5, 4, 5, 5, 6, 4, 5, 5, 6, 5, 6, 6, 7, 4, 5, 5, 6, 5, 6, 6, 7, 5, 6, 6, 7, 6, 7, 7, 8,
        };

    /// <summary>
    /// Returns a percentage-based similarity value between the two given hashes. The higher
    /// the percentage, the closer the hashes are to being identical.
    /// </summary>
    /// <param name="hash1">The first hash.</param>
    /// <param name="hash2">The second hash.</param>
    /// <returns>The similarity percentage.</returns>
    public static double Similarity(ulong hash1, ulong hash2)
    {
        return (64 - BitCount(hash1 ^ hash2)) * 100 / 64.0;
    }

    /// <summary>
    /// Returns a percentage-based similarity value between the two given hashes. The higher
    /// the percentage, the closer the hashes are to being identical.
    /// </summary>
    /// <param name="hash1">The first hash. Cannot be null and must have a length of 8.</param>
    /// <param name="hash2">The second hash. Cannot be null and must have a length of 8.</param>
    /// <returns>The similarity percentage.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hash1"/> or <paramref name="hash2"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="hash1"/> or <paramref name="hash2"/> has a length other than <c>8</c>.</exception>
    public static double Similarity(byte[] hash1, byte[] hash2)
    {
        if (hash1 == null)
        {
            throw new ArgumentNullException(nameof(hash1));
        }

        if (hash2 == null)
        {
            throw new ArgumentNullException(nameof(hash2));
        }

        if (hash1.Length != 8)
        {
            throw new ArgumentOutOfRangeException(nameof(hash1));
        }

        if (hash2.Length != 8)
        {
            throw new ArgumentOutOfRangeException(nameof(hash2));
        }

        var h1 = BitConverter.ToUInt64(hash1, 0);
        var h2 = BitConverter.ToUInt64(hash2, 0);
        return Similarity(h1, h2);
    }

    /// <summary>Counts bits Utility function for similarity.</summary>
    /// <param name="num">The hash we are counting.</param>
    /// <returns>The total bit count.</returns>
    private static uint BitCount(ulong num)
    {
        uint count = 0;
        for (; num > 0; num >>= 8)
        {
            count += bitCounts[num & 0xff];
        }

        return count;
    }
}