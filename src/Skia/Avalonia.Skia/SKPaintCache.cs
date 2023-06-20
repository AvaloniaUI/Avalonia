using System.Collections.Concurrent;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Cache for SKPaints.
    /// </summary>
    internal class SKPaintCache : SKCacheBase<SKPaint, SKPaintCache>
    {
        /// <summary>
        /// Returns a SKPaint and resets it for reuse later.
        /// </summary>
        /// <remarks>
        /// Do not use the paint further.
        /// Do not return the same paint multiple times as that will break the cache.
        /// Uses SKPaint.Reset() for reuse later.
        /// </remarks>
        /// <param name="paint">Paint to reset.</param>
        public void ReturnReset(SKPaint paint)
        {
            paint.Reset();
            Cache.Add(paint);
        }
    }
}
