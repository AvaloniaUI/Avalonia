using System;
using Avalonia.Base.UnitTests.Media.Fonts.Tables;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting;

public class ShapedBufferTests
{
    private const string InterFontUri =
        "resm:Avalonia.Base.UnitTests.Assets.Inter-Regular.ttf?assembly=Avalonia.Base.UnitTests";

    private static GlyphTypeface LoadInter()
    {
        var assetLoader = new StandardAssetLoader();
        using var stream = assetLoader.Open(new Uri(InterFontUri));
        return new GlyphTypeface(new CustomPlatformTypeface(stream));
    }

    [Fact]
    public void GlyphIndices_Length_Matches_BufferLength()
    {
        var typeface = LoadInter();

        using var buffer = new ShapedBuffer("hello".AsMemory(), 5, typeface, 16, 0);

        Assert.Equal(5, buffer.GlyphIndices.Length);
        Assert.Equal(5, buffer.Length);
    }

    [Fact]
    public void Indexer_Set_Syncs_GlyphIndices()
    {
        var typeface = LoadInter();

        using var buffer = new ShapedBuffer("ABC".AsMemory(), 3, typeface, 16, 0);

        buffer[0] = new GlyphInfo(42, 0, 10);
        buffer[1] = new GlyphInfo(99, 1, 11);
        buffer[2] = new GlyphInfo(7,  2, 12);

        Assert.Equal(42, buffer.GlyphIndices[0]);
        Assert.Equal(99, buffer.GlyphIndices[1]);
        Assert.Equal(7,  buffer.GlyphIndices[2]);

        // And the GlyphInfo accessor still works.
        Assert.Equal(42, buffer[0].GlyphIndex);
        Assert.Equal(99, buffer[1].GlyphIndex);
        Assert.Equal(7,  buffer[2].GlyphIndex);
    }

    [Fact]
    public void Indexer_Set_Overwrite_Updates_GlyphIndices()
    {
        var typeface = LoadInter();

        using var buffer = new ShapedBuffer("X".AsMemory(), 1, typeface, 16, 0);

        buffer[0] = new GlyphInfo(10, 0, 1);
        Assert.Equal(10, buffer.GlyphIndices[0]);

        // Mutating the entry (as InterWordJustification does) keeps the parallel array in sync.
        buffer[0] = new GlyphInfo(10, 0, 2);
        Assert.Equal(10, buffer.GlyphIndices[0]);

        buffer[0] = new GlyphInfo(33, 0, 1);
        Assert.Equal(33, buffer.GlyphIndices[0]);
    }

    [Fact]
    public void Split_Ascending_Preserves_GlyphIndices_Alignment()
    {
        var typeface = LoadInter();

        // LTR (bidi 0): clusters ascending. Five 1-char clusters.
        using var buffer = new ShapedBuffer("ABCDE".AsMemory(), 5, typeface, 16, 0);
        buffer[0] = new GlyphInfo(10, 0, 5);
        buffer[1] = new GlyphInfo(20, 1, 5);
        buffer[2] = new GlyphInfo(30, 2, 5);
        buffer[3] = new GlyphInfo(40, 3, 5);
        buffer[4] = new GlyphInfo(50, 4, 5);

        var split = buffer.Split(2);

        Assert.NotNull(split.First);
        Assert.NotNull(split.Second);
        Assert.Equal(2, split.First!.GlyphIndices.Length);
        Assert.Equal(3, split.Second!.GlyphIndices.Length);

        Assert.Equal(10, split.First.GlyphIndices[0]);
        Assert.Equal(20, split.First.GlyphIndices[1]);

        Assert.Equal(30, split.Second.GlyphIndices[0]);
        Assert.Equal(40, split.Second.GlyphIndices[1]);
        Assert.Equal(50, split.Second.GlyphIndices[2]);

        // Both halves stay in lockstep with the GlyphInfo indexer.
        Assert.Equal(split.First[0].GlyphIndex,  split.First.GlyphIndices[0]);
        Assert.Equal(split.Second[1].GlyphIndex, split.Second.GlyphIndices[1]);
    }

    [Fact]
    public void Split_At_Zero_Yields_Empty_Leading_With_Empty_Indices()
    {
        var typeface = LoadInter();

        using var buffer = new ShapedBuffer("ABC".AsMemory(), 3, typeface, 16, 0);
        buffer[0] = new GlyphInfo(10, 0, 5);
        buffer[1] = new GlyphInfo(20, 1, 5);
        buffer[2] = new GlyphInfo(30, 2, 5);

        var split = buffer.Split(0);

        Assert.NotNull(split.First);
        Assert.Equal(0, split.First!.GlyphIndices.Length);
        Assert.Equal(0, split.First.Length);
        Assert.NotNull(split.Second);
        Assert.Equal(3, split.Second!.GlyphIndices.Length);
    }

    [Fact]
    public void Dispose_Clears_GlyphIndices_View()
    {
        var typeface = LoadInter();

        var buffer = new ShapedBuffer("AB".AsMemory(), 2, typeface, 16, 0);
        buffer[0] = new GlyphInfo(1, 0, 1);
        buffer[1] = new GlyphInfo(2, 1, 1);

        Assert.Equal(2, buffer.GlyphIndices.Length);

        buffer.Dispose();

        // After dispose the views are reset so we don't reach into the returned pool buffer.
        Assert.Equal(0, buffer.GlyphIndices.Length);
        Assert.Equal(0, buffer.Length);
    }
}
