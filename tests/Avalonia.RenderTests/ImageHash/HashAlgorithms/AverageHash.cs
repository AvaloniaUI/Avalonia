using System;
using SkiaSharp;

namespace Avalonia.UnitTests;

public class AverageHash : IImageHash
{
    private const int WIDTH = 8;
    private const int HEIGHT = 8;
    private const int NRPIXELS = WIDTH * HEIGHT;
    private const ulong MOSTSIGNIFICANTBITMASK = 1UL << (NRPIXELS - 1);

    /// <inheritdoc />
    public ulong Hash(SKBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }

        // Resize the bitmap
        using (SKBitmap resizedBitmap = bitmap.Resize(new SKImageInfo(WIDTH, HEIGHT), SKFilterQuality.High))
        {
            // Convert the bitmap to grayscale
            using (SKBitmap grayscaleBitmap = resizedBitmap.ConvertToGrayscale())
            {
                var hash = 0UL;

                // Compute the average value
                var averageValue = 0U;
                for (var y = 0; y < HEIGHT; y++)
                {
                    Span<byte> row = grayscaleBitmap.GetPixelRow(y);
                    for (var x = 0; x < WIDTH; x++)
                    {
                        averageValue += row[x];
                    }
                }

                averageValue /= NRPIXELS;

                // Compute the hash: each bit is a pixel
                // 1 = higher than average, 0 = lower than average
                var mask = MOSTSIGNIFICANTBITMASK;

                for (var y = 0; y < HEIGHT; y++)
                {
                    Span<byte> row = grayscaleBitmap.GetPixelRow(y);
                    for (var x = 0; x < WIDTH; x++)
                    {
                        if (row[x] >= averageValue)
                        {
                            hash |= mask;
                        }

                        mask >>= 1;
                    }
                }

                return hash;
            }
        }
    }
}