using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Skia;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    /// <summary>
    /// Exercises the ref-counted shared storage and the per-write generation
    /// counter that <see cref="ShapedBuffer.Split"/> and
    /// <see cref="ShapedBuffer.WithBidiLevel"/> rely on. Each test starts from
    /// a freshly shaped buffer (so the glyph and cluster arrays are pool-rented
    /// and ref-counted) and checks that:
    /// <list type="bullet">
    ///   <item>aliases keep working after their source is disposed,</item>
    ///   <item><see cref="ShapedBuffer.Dispose"/> is idempotent,</item>
    ///   <item>indexer mutations propagate to siblings via the generation bump
    ///         (i.e. nobody is left with a stale cluster cache).</item>
    /// </list>
    /// </summary>
    public class ShapedBufferSharedStorageTests
    {
        private const string AsciiText = "The quick brown fox";

        [Fact]
        public void Dispose_Is_Idempotent()
        {
            using (Start())
            {
                var buffer = ShapeAscii(AsciiText);
                _ = buffer.TotalGlyphAdvance; // prime cluster cache so refs are non-null.

                buffer.Dispose();
                buffer.Dispose();
                buffer.Dispose();
            }
        }

        [Fact]
        public void Split_Children_Survive_Parent_Disposal()
        {
            using (Start())
            {
                var parent = ShapeAscii(AsciiText);
                var totalBefore = parent.TotalGlyphAdvance;

                var split = parent.Split(parent.Text.Length / 2);
                var first = split.First!;
                var second = split.Second!;

                // Prime sibling caches via the parent's cluster-cache reference
                // (children inherit the parent's prefix sums by ref).
                _ = first.TotalGlyphAdvance;
                _ = second.TotalGlyphAdvance;

                // Release the parent's references first; the ref-counted holders
                // must keep the pool arrays alive for the surviving children.
                parent.Dispose();

                Assert.Equal(totalBefore, first.TotalGlyphAdvance + second.TotalGlyphAdvance, 3);
                Assert.Equal(parent.Text.Length / 2, first.Text.Length);
                Assert.True(first.Length > 0);
                Assert.True(second.Length > 0);

                first.Dispose();
                second.Dispose();
            }
        }

        [Fact]
        public void Split_Parent_Survives_Children_Disposal()
        {
            using (Start())
            {
                using var parent = ShapeAscii(AsciiText);
                var totalBefore = parent.TotalGlyphAdvance;

                var split = parent.Split(parent.Text.Length / 2);
                split.First!.Dispose();
                split.Second!.Dispose();

                // Parent's own refs must still hold the pool arrays alive.
                Assert.Equal(totalBefore, parent.TotalGlyphAdvance, 6);
            }
        }

        [Fact]
        public void WithBidiLevel_Alias_Survives_Original_Disposal()
        {
            using (Start())
            {
                var original = ShapeAscii(AsciiText);
                var totalBefore = original.TotalGlyphAdvance;
                Assert.Equal(0, original.BidiLevel);

                using var alias = original.WithBidiLevel(2);

                original.Dispose();

                Assert.Equal(totalBefore, alias.TotalGlyphAdvance, 6);
                Assert.Equal(2, alias.BidiLevel);
                Assert.Equal(original.Text.Length, alias.Text.Length);
            }
        }

        [Fact]
        public void WithBidiLevel_Returns_Same_Instance_When_Level_Matches()
        {
            using (Start())
            {
                using var original = ShapeAscii(AsciiText);
                var alias = original.WithBidiLevel(original.BidiLevel);
                Assert.Same(original, alias);
            }
        }

        [Fact]
        public void IndexerMutation_Invalidates_Own_Cluster_Cache()
        {
            using (Start())
            {
                using var buffer = ShapeAscii(AsciiText);

                var advanceBefore = buffer.TotalGlyphAdvance;
                var original = buffer[0];

                // Mutate the leading glyph's advance via the indexer setter.
                const double delta = 50d;
                buffer[0] = new GlyphInfo(
                    original.GlyphIndex, original.GlyphCluster,
                    original.GlyphAdvance + delta, original.GlyphOffset);

                // The cache must be rebuilt against the new glyph data.
                Assert.Equal(advanceBefore + delta, buffer.TotalGlyphAdvance, 3);
            }
        }

        [Fact]
        public void IndexerMutation_On_Parent_Invalidates_Sibling_Caches_After_Split()
        {
            using (Start())
            {
                using var parent = ShapeAscii(AsciiText);

                var splitIndex = parent.Text.Length / 2;
                var split = parent.Split(splitIndex);
                using var first = split.First!;
                using var second = split.Second!;

                // Prime both children's views of the inherited cluster cache.
                var firstBefore = first.TotalGlyphAdvance;
                var secondBefore = second.TotalGlyphAdvance;

                // Mutate parent[0]: this lives in the first child's slice but
                // bumps the generation counter on the shared glyph holder so
                // the second child also sees the change (and rebuilds if needed).
                const double delta = 25d;
                var first0 = parent[0];
                parent[0] = new GlyphInfo(
                    first0.GlyphIndex, first0.GlyphCluster,
                    first0.GlyphAdvance + delta, first0.GlyphOffset);

                Assert.Equal(firstBefore + delta, first.TotalGlyphAdvance, 3);
                // Second child's range doesn't include glyph 0 so its advance is unchanged,
                // but its cache must still have been invalidated/rebuilt without error.
                Assert.Equal(secondBefore, second.TotalGlyphAdvance, 3);
            }
        }

        [Fact]
        public void IndexerMutation_On_Child_Invalidates_Parent_And_Sibling()
        {
            using (Start())
            {
                using var parent = ShapeAscii(AsciiText);

                var splitIndex = parent.Text.Length / 2;
                var split = parent.Split(splitIndex);
                using var first = split.First!;
                using var second = split.Second!;

                var parentBefore = parent.TotalGlyphAdvance;
                _ = first.TotalGlyphAdvance;
                _ = second.TotalGlyphAdvance;

                const double delta = 17d;
                var glyph = first[0];
                first[0] = new GlyphInfo(
                    glyph.GlyphIndex, glyph.GlyphCluster,
                    glyph.GlyphAdvance + delta, glyph.GlyphOffset);

                Assert.Equal(parentBefore + delta, parent.TotalGlyphAdvance, 3);
            }
        }

        [Fact]
        public void IndexerMutation_Invalidates_WithBidiLevel_Alias_Cache()
        {
            using (Start())
            {
                using var original = ShapeAscii(AsciiText);
                using var alias = original.WithBidiLevel(2);

                var aliasBefore = alias.TotalGlyphAdvance;

                const double delta = 33d;
                var glyph = original[0];
                original[0] = new GlyphInfo(
                    glyph.GlyphIndex, glyph.GlyphCluster,
                    glyph.GlyphAdvance + delta, glyph.GlyphOffset);

                Assert.Equal(aliasBefore + delta, alias.TotalGlyphAdvance, 3);
            }
        }

        // Regression: when a buffer is mutated *before* it is Split / aliased,
        // the parent's _cacheGeneration is already > 0 by the time the alias
        // inherits the cluster cache. The alias constructor must copy that
        // generation onto the child; otherwise the child's stamp stays at 0,
        // EnsureClusterCache sees a mismatch on first access and rebuilds a
        // fresh cache instead of reusing the parent's pooled arrays — silently
        // defeating the cached-split fast path.
        [Fact]
        public void Split_Children_Share_Parent_Cluster_Cache_When_Parent_Was_Mutated_Before_Split()
        {
            using (Start())
            {
                using var parent = ShapeAscii(AsciiText);

                // Mutate first so the shared glyph holder's generation is
                // non-zero before the cluster cache is built.
                var first0 = parent[0];
                parent[0] = new GlyphInfo(
                    first0.GlyphIndex, first0.GlyphCluster,
                    first0.GlyphAdvance + 10d, first0.GlyphOffset);

                // Prime the parent's cluster cache against the bumped generation.
                _ = parent.TotalGlyphAdvance;
                var parentPrefix = parent.ClusterPrefix;
                Assert.NotNull(parentPrefix);

                var split = parent.Split(parent.Text.Length / 2);
                using var first = split.First!;
                using var second = split.Second!;

                // Touching the child's metrics must reuse the parent's prefix
                // array, not rebuild a fresh one.
                _ = first.TotalGlyphAdvance;
                _ = second.TotalGlyphAdvance;

                Assert.Same(parentPrefix, first.ClusterPrefix);
                Assert.Same(parentPrefix, second.ClusterPrefix);
            }
        }

        [Fact]
        public void WithBidiLevel_Alias_Shares_Cluster_Cache_When_Original_Was_Mutated_Before_Alias()
        {
            using (Start())
            {
                using var original = ShapeAscii(AsciiText);

                var first0 = original[0];
                original[0] = new GlyphInfo(
                    first0.GlyphIndex, first0.GlyphCluster,
                    first0.GlyphAdvance + 7d, first0.GlyphOffset);

                _ = original.TotalGlyphAdvance;
                var originalPrefix = original.ClusterPrefix;
                Assert.NotNull(originalPrefix);

                using var alias = original.WithBidiLevel(2);

                _ = alias.TotalGlyphAdvance;

                Assert.Same(originalPrefix, alias.ClusterPrefix);
            }
        }

        private static ShapedBuffer ShapeAscii(string text)
        {
            var options = new TextShaperOptions(
                Typeface.Default.GlyphTypeface, 12, 0, CultureInfo.CurrentCulture);
            return TextShaper.Current.ShapeText(text, options);
        }

        private static IDisposable Start()
        {
            return UnitTestApplication.Start(TestServices.MockPlatformRenderInterface
                .With(renderInterface: new PlatformRenderInterface(null),
                    fontManagerImpl: new CustomFontManagerImpl()));
        }
    }
}
