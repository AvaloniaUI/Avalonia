using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Cache for SKRoundRectCache.
    /// </summary>
    internal class SKRoundRectCache : SKCacheBase<SKRoundRect, SKRoundRectCache>
    {
        /// <summary>
        /// Cache for points to use for setting the radii.
        /// </summary>
        private readonly ConcurrentBag<SKPoint[]> _radiiCache = new();

        /// <summary>
        /// Gets a cached SKRoundRect and sets it with the passed rectangle and Radii.
        /// </summary>
        /// <param name="rectangle">Rectangle size to set the cached rectangle to.</param>
        /// <param name="roundedRect">Rounded rectangle to copy the radii from.</param>
        /// <returns>Configured rounded rectangle</returns>
        public SKRoundRect GetAndSetRadii(in SKRect rectangle, in RoundedRect roundedRect)
        {
            if (!Cache.TryTake(out var item))
            {
                item = new SKRoundRect();
            }
            
            // Try and acquire a cached point array.
            if (!_radiiCache.TryTake(out var skArray))
            {
                skArray = new SKPoint[4];
            }

            skArray[0].X = (float)roundedRect.RadiiTopLeft.X;
            skArray[0].Y = (float)roundedRect.RadiiTopLeft.Y;
            skArray[1].X = (float)roundedRect.RadiiTopRight.X;
            skArray[1].Y = (float)roundedRect.RadiiTopRight.Y;
            skArray[2].X = (float)roundedRect.RadiiBottomRight.X;
            skArray[2].Y = (float)roundedRect.RadiiBottomRight.Y;
            skArray[3].X = (float)roundedRect.RadiiBottomLeft.X;
            skArray[3].Y = (float)roundedRect.RadiiBottomLeft.Y;

            item.SetRectRadii(rectangle, skArray);

            // Add the array back to the cache.
            _radiiCache.Add(skArray);

            return item;
        }

        /// <summary>
        /// Gets a cached SKRoundRect and sets it with the passed rectangle and Radii.
        /// </summary>
        /// <param name="rectangle">Rectangle size to set the cached rectangle to.</param>
        /// <param name="radii">point array of radii.</param>
        /// <returns>Configured rounded rectangle</returns>
        public SKRoundRect GetAndSetRadii(in SKRect rectangle, in SKPoint[] radii)
        {
            if (!Cache.TryTake(out var item))
            {
                item = new SKRoundRect();
            }

            item.SetRectRadii(rectangle, radii);

            return item;
        }
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

        /// <summary>
        /// Clears and disposes all cached items.
        /// </summary>
        public new void Clear()
        {
            base.Clear();

            // Clear out the cache of SKPoint arrays.
            while (_radiiCache.TryTake(out var item))
            {
            }
        }
    }
}
