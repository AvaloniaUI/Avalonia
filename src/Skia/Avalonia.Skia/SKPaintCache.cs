using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal static class SKPaintCache
    {
        private static ConcurrentBag<SKPaint> s_cachedPaints;

        static SKPaintCache()
        {
            s_cachedPaints = new ConcurrentBag<SKPaint>();
        }

        public static SKPaint Get()
        {
            if (!s_cachedPaints.TryTake(out var paint))
            {
                paint = new SKPaint();
            }

            return paint;
        }

        public static void Return(SKPaint paint)
        {
            paint.Reset();
            s_cachedPaints.Add(paint);
        }

        public static void Clear()
        {
            while (s_cachedPaints.TryTake(out var paint))
            {
                paint.Dispose();
            }
        }

    }
}
