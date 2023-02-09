using System.Collections.Concurrent;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Cache for SKPaints.
    /// </summary>
    internal class SKRoundRectCache : SKCacheBase<SKRoundRect, SKRoundRectCache>
    {
        /// <summary>
        /// Returns a SKPaint and resets it for reuse later.
        /// </summary>
        /// <remarks>
        /// Do not use the rect further.
        /// Do not return the same rect multiple times as that will break the cache.
        /// Uses SKRoundRect.SetEmpty(); for reuse later.
        /// </remarks>
        /// <param name="rect">Rectangle to reset</param>
        public void ReturnReset(SKRoundRect rect)
        {
            rect.SetEmpty();
            Cache.Add(rect);
        }
    }
}
