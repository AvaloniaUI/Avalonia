using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    /// <summary>
    /// <see cref="Avalonia.Media.GlyphTypeface"/> deliberately does <b>not</b> tear down its
    /// per-instance glyph cache on <see cref="Avalonia.Media.GlyphTypeface.Dispose"/>. Cache payloads
    /// are handed out lock-free and can escape into retained compositor render data that outlives the
    /// typeface, so clearing or disposing the cache would risk a use-after-free; the cache owns no
    /// unmanaged handles and is reclaimed by the GC together with the typeface.
    /// </summary>
    public class GlyphTypefaceLifecycleTests
    {
        [Fact]
        public void Dispose_Leaves_The_Glyph_Cache_For_The_Gc()
        {
            // A CFF font: its ink-bounds path interprets the charstring into a box and populates the
            // glyph cache without needing a render backend.
            var typeface = SyntheticFont.FromAsset(SyntheticFont.Assets.CffTest).TryCreateGlyphTypeface();
            Assert.NotNull(typeface);

            var glyph = typeface!.CharacterToGlyphMap['I'];
            Assert.True(typeface.TryGetGlyphMetrics(glyph, out _));

            // The cache now holds at least one entry.
            Assert.NotNull(typeface.GlyphCache);
            Assert.True(typeface.GlyphCache!.Count > 0);

            typeface.Dispose();

            // Dispose neither clears nor nulls the cache: a payload handed out before disposal stays
            // valid, and the cache is left intact for the GC to reclaim with the typeface.
            Assert.NotNull(typeface.GlyphCache);
            Assert.True(typeface.GlyphCache!.Count > 0);
        }
    }
}
