using System;
using Avalonia;
using Xunit;

namespace Avalonia.Wayland.UnitTests;

public class LayerShellOptionsTests
{
    [Fact]
    public void Defaults_match_a_passive_overlay()
    {
        var options = new LayerShellOptions
        {
            Width = 300,
            Height = 38,
        };

        Assert.Equal(LayerShellLayer.Overlay, options.Layer);
        Assert.Equal(LayerShellAnchor.Top, options.Anchor);
        Assert.Equal(-1, options.ExclusiveZone);
        Assert.Equal(default, options.Margin);
        Assert.Equal(LayerShellKeyboardInteractivity.None, options.KeyboardInteractivity);
        Assert.Equal("avalonia", options.Namespace);
    }

    [Fact]
    public void Explicit_values_are_preserved()
    {
        var margin = new Thickness(1, 2, 3, 4);
        var options = new LayerShellOptions
        {
            Layer = LayerShellLayer.Bottom,
            Anchor = LayerShellAnchor.Left | LayerShellAnchor.Right,
            ExclusiveZone = 0,
            Margin = margin,
            KeyboardInteractivity = LayerShellKeyboardInteractivity.OnDemand,
            Namespace = "test-surface",
            Width = 1920,
            Height = 32,
        };

        Assert.Equal(LayerShellLayer.Bottom, options.Layer);
        Assert.Equal(LayerShellAnchor.Left | LayerShellAnchor.Right, options.Anchor);
        Assert.Equal(0, options.ExclusiveZone);
        Assert.Equal(margin, options.Margin);
        Assert.Equal(LayerShellKeyboardInteractivity.OnDemand, options.KeyboardInteractivity);
        Assert.Equal("test-surface", options.Namespace);
        Assert.Equal(1920, options.Width);
        Assert.Equal(32, options.Height);
    }

    [Fact]
    public void CreateWindowImpl_rejects_null_options()
    {
        Assert.Throws<ArgumentNullException>(
            () => WaylandLayerShell.CreateWindowImpl(null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CreateWindowImpl_rejects_invalid_width(double width)
    {
        var options = new LayerShellOptions
        {
            Width = width,
            Height = 38,
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => WaylandLayerShell.CreateWindowImpl(options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void CreateWindowImpl_rejects_invalid_height(double height)
    {
        var options = new LayerShellOptions
        {
            Width = 300,
            Height = height,
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            () => WaylandLayerShell.CreateWindowImpl(options));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateWindowImpl_rejects_empty_namespace(string namespaceName)
    {
        var options = new LayerShellOptions
        {
            Width = 300,
            Height = 38,
            Namespace = namespaceName,
        };

        Assert.Throws<ArgumentException>(
            () => WaylandLayerShell.CreateWindowImpl(options));
    }
}
