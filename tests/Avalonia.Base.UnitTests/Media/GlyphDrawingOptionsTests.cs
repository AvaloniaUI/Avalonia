using System;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Base.UnitTests.Media
{
    public class GlyphDrawingOptionsTests
    {
        [Fact]
        public void Default_Has_Null_PaletteIndex_And_Null_PixelSize()
        {
            var options = GlyphDrawingOptions.Default;

            Assert.Null(options.PaletteIndex);
            Assert.Null(options.PixelSize);
        }

        [Fact]
        public void Default_Is_A_Singleton()
        {
            Assert.Same(GlyphDrawingOptions.Default, GlyphDrawingOptions.Default);
        }

        [Fact]
        public void Parameterless_Constructor_Produces_An_Instance_Equal_To_Default()
        {
            // The record's equality contract should treat a freshly-constructed
            // instance with no overrides as equal to the Default singleton, even
            // though they're distinct instances. This keeps callers from having
            // to compare against Default by reference.
            var fresh = new GlyphDrawingOptions();

            Assert.NotSame(GlyphDrawingOptions.Default, fresh);
            Assert.Equal(GlyphDrawingOptions.Default, fresh);
            Assert.Equal(GlyphDrawingOptions.Default.GetHashCode(), fresh.GetHashCode());
        }

        [Fact]
        public void PaletteIndex_Accepts_Null()
        {
            var options = new GlyphDrawingOptions { PaletteIndex = null };

            Assert.Null(options.PaletteIndex);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(99)]
        public void PaletteIndex_Accepts_NonNegative_Values(int value)
        {
            var options = new GlyphDrawingOptions { PaletteIndex = value };

            Assert.Equal(value, options.PaletteIndex);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void PaletteIndex_Rejects_Negative_Values(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GlyphDrawingOptions { PaletteIndex = value });
        }

        [Fact]
        public void PixelSize_Accepts_Null()
        {
            var options = new GlyphDrawingOptions { PixelSize = null };

            Assert.Null(options.PixelSize);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(16)]
        [InlineData(256)]
        public void PixelSize_Accepts_Positive_Values(int value)
        {
            var options = new GlyphDrawingOptions { PixelSize = value };

            Assert.Equal(value, options.PixelSize);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void PixelSize_Rejects_Values_Below_One(int value)
        {
            // A pixel size of zero is meaningless for a bitmap strike; reject it at
            // construction time rather than letting it propagate to renderer code.
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GlyphDrawingOptions { PixelSize = value });
        }

        [Fact]
        public void Equality_Is_Structural_Across_Two_Records_With_Same_Values()
        {
            var a = new GlyphDrawingOptions { PaletteIndex = 1, PixelSize = 16 };
            var b = new GlyphDrawingOptions { PaletteIndex = 1, PixelSize = 16 };

            Assert.NotSame(a, b);
            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equality_Distinguishes_Different_PaletteIndex()
        {
            var a = new GlyphDrawingOptions { PaletteIndex = 0 };
            var b = new GlyphDrawingOptions { PaletteIndex = 1 };

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Equality_Distinguishes_Different_PixelSize()
        {
            var a = new GlyphDrawingOptions { PixelSize = 16 };
            var b = new GlyphDrawingOptions { PixelSize = 32 };

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void With_Expression_Produces_A_Modified_Copy()
        {
            // `record` participation is part of the public contract; ensure callers
            // can use `with` to derive a tweaked instance.
            var original = new GlyphDrawingOptions { PaletteIndex = 1, PixelSize = 16 };
            var tweaked = original with { PaletteIndex = 2 };

            Assert.Equal(1, original.PaletteIndex);
            Assert.Equal(2, tweaked.PaletteIndex);
            Assert.Equal(16, tweaked.PixelSize);
            Assert.NotEqual(original, tweaked);
        }

        [Fact]
        public void With_Expression_Validates_New_Values()
        {
            var original = new GlyphDrawingOptions { PaletteIndex = 1 };

            Assert.Throws<ArgumentOutOfRangeException>(() => original with { PaletteIndex = -5 });
        }
    }
}
