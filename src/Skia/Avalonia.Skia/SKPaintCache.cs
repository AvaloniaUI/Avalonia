using System.Collections.Concurrent;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Cache for SKPaints.
    /// </summary>
    internal static class SKPaintCache
    {
        private static ConcurrentBag<SKPaint> s_cachedPaints;

        static SKPaintCache()
        {
            s_cachedPaints = new ConcurrentBag<SKPaint>();
        }

        /// <summary>
        /// Gets a SKPaint for usage.
        /// </summary>
        /// <remarks>
        /// If a SKPaint is in the cache, that existing SKPaint will be returned.
        /// Otherwise a new SKPaint will be created.
        /// </remarks>
        /// <returns></returns>
        public static SKPaint Get()
        {
            if (!s_cachedPaints.TryTake(out var paint))
            {
                paint = new SKPaint();
            }

            return paint;
        }

        /// <summary>
        /// Returns a SKPaint for reuse later.
        /// </summary>
        /// <remarks>
        /// Do not use the paint further.
        /// Do not return the same paint multiple times as that will break the cache.
        /// </remarks>
        /// <param name="paint"></param>
        public static void Return(SKPaint paint)
        {
            s_cachedPaints.Add(paint);
        }

        /// <summary>
        /// Returns a SKPaint and resets it for reuse later.
        /// </summary>
        /// <remarks>
        /// Do not use the paint further.
        /// Do not return the same paint multiple times as that will break the cache.
        /// Uses SKPaint.Reset() for reuse later.
        /// </remarks>
        /// <param name="paint"></param>
        public static void ReturnReset(SKPaint paint)
        {
            paint.Reset();
            s_cachedPaints.Add(paint);
        }

        /// <summary>
        /// Clears and disposes all cached paints.
        /// </summary>
        public static void Clear()
        {
            while (s_cachedPaints.TryTake(out var paint))
            {
                paint.Dispose();
            }
        }

    }
}
