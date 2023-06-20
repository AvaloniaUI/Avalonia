using System;
using System.Collections.Concurrent;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Cache base for Skia objects.
    /// </summary>
    internal abstract class SKCacheBase<TCachedItem, TCache>
        where TCachedItem : IDisposable, new()
        where TCache : new()
    {
        /// <summary>
        /// Bag to hold the cached items.
        /// </summary>
        protected readonly ConcurrentBag<TCachedItem> Cache;

        /// <summary>
        /// Shared cache.
        /// </summary>
        public static readonly TCache Shared = new TCache();

        protected SKCacheBase()
        {
            Cache = new ConcurrentBag<TCachedItem>();
        }

        /// <summary>
        /// Gets a cached item for usage.
        /// </summary>
        /// <remarks>
        /// If there is a available item in the cache, the cached item will be returned..
        /// Otherwise a new cached item will be created.
        /// </remarks>
        /// <returns></returns>
        public TCachedItem Get()
        {
            if (!Cache.TryTake(out var item))
            {
                item = new TCachedItem();
            }

            return item;
        }

        /// <summary>
        /// Returns the item for reuse later.
        /// </summary>
        /// <remarks>
        /// Do not use the item further.
        /// Do not return the same item multiple times as that will break the cache.
        /// </remarks>
        /// <param name="item"></param>
        public void Return(TCachedItem item)
        {
            Cache.Add(item);
        }

        /// <summary>
        /// Clears and disposes all cached items.
        /// </summary>
        public void Clear()
        {
            while (Cache.TryTake(out var item))
            {
                item.Dispose();
            }
        }

    }
}
