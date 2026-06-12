using System;
using System.Threading;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    /// <summary>
    /// <see cref="GlyphTypeface.TextShaperTypeface"/>'s lazy getter is synchronized, so concurrent
    /// first access creates exactly one shaper instead of racing to build several and caching (then
    /// leaking) all but the last.
    /// </summary>
    public class GlyphTypefaceThreadSafetyTests
    {
        [Fact]
        public void Concurrent_First_Access_To_TextShaperTypeface_Creates_One_Shaper()
        {
            const int threadCount = 8;
            var createCount = 0;

            // The mock counts CreateTypeface calls and dwells briefly inside each one. The dwell widens
            // the first-access window so that, were the getter ever left unsynchronized again, multiple
            // racing threads would slip past the null-check and the assertion below would catch it.
            var shaper = new CountingTextShaperImpl(
                onCreate: () => Interlocked.Increment(ref createCount));

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(textShaperImpl: shaper)))
            {
                var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.InterRegular).TryCreateGlyphTypeface();
                Assert.NotNull(typeface);

                using var ready = new CountdownEvent(threadCount);
                using var start = new ManualResetEventSlim(false);

                var threads = new Thread[threadCount];
                for (var i = 0; i < threadCount; i++)
                {
                    threads[i] = new Thread(() =>
                    {
                        ready.Signal();
                        start.Wait();
                        _ = typeface!.TextShaperTypeface;
                    });
                }

                foreach (var thread in threads)
                {
                    thread.Start();
                }

                // Release every worker at once, after they are all parked on the start gate, so the
                // first-access race is as tight as possible.
                Assert.True(ready.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken),
                    "Worker threads did not become ready.");
                start.Set();

                foreach (var thread in threads)
                {
                    Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "A worker thread did not complete.");
                }

                // The synchronized getter creates exactly one shaper no matter how many threads race
                // into the first access.
                Assert.Equal(1, createCount);
            }
        }

        private sealed class CountingTextShaperImpl : ITextShaperImpl
        {
            private readonly Action _onCreate;

            public CountingTextShaperImpl(Action onCreate)
            {
                _onCreate = onCreate;
            }

            public ITextShaperTypeface CreateTypeface(GlyphTypeface glyphTypeface)
            {
                _onCreate();
                Thread.Sleep(50);
                return new NoOpShaperTypeface();
            }

            public ShapedBuffer ShapeText(ReadOnlyMemory<char> text, TextShaperOptions options)
                => throw new NotSupportedException();
        }

        private sealed class NoOpShaperTypeface : ITextShaperTypeface
        {
            public void Dispose() { }
        }
    }
}
