using System;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Imaging
{
    public class BitmapSaveTests
    {
        [Fact]
        public void Bitmap_Should_Not_Be_Disposed_While_Saving()
        {
            var impl = new CollectingBitmapImpl();

            Save(impl);

            Assert.True(impl.Saved);
            Assert.False(impl.DisposedDuringSave);
        }

        [Fact]
        public void Bitmap_Should_Not_Be_Disposed_While_Saving_With_Quality()
        {
            var impl = new CollectingBitmapImpl();

            SaveWithQuality(impl);

            Assert.True(impl.Saved);
            Assert.False(impl.DisposedDuringSave);
        }

        // Optimized code may treat the bitmap as dead once Save has read PlatformImpl. AggressiveOptimization
        // makes this happen on the first call instead of only after tiered compilation has optimized Save.
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void Save(IBitmapImpl impl)
            => new Bitmap(RefCountable.Create(impl)).Save(new MemoryStream(), PngBitmapEncoderOptions.Default);

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
        private static void SaveWithQuality(IBitmapImpl impl)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            new Bitmap(RefCountable.Create(impl)).Save(new MemoryStream(), 80);
#pragma warning restore CS0618
        }

        private class CollectingBitmapImpl : IBitmapImpl
        {
            private bool _disposed;

            public Vector Dpi => new(96, 96);
            public PixelSize PixelSize => new(1, 1);
            public int Version => 0;
            public bool Saved { get; private set; }
            public bool DisposedDuringSave { get; private set; }

            public void Save(Stream stream, BitmapEncoderOptions options)
            {
                // Native encoders read the pixels here, so the bitmap must not be disposed yet.
                GC.Collect();
                GC.WaitForPendingFinalizers();
                DisposedDuringSave = _disposed;
                Saved = true;
            }

            public void Dispose() => _disposed = true;
        }
    }
}
