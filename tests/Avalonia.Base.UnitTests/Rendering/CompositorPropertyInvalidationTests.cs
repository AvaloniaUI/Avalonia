using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

// A change to a property that the compositor applies to the composition visual must update that visual without
// rendering the content again. A new draw list is a content change for the compositor (for example, it redraws
// a bitmap cache).
public class CompositorPropertyInvalidationTests : CompositorTestsBase
{
    private sealed class CountingControl : Control
    {
        public int RenderCount { get; private set; }

        public override void Render(DrawingContext context)
        {
            RenderCount++;
            context.FillRectangle(Brushes.Red, new Rect(Bounds.Size));
        }
    }

    public enum Change
    {
        Opacity,
        IsVisible,
        ClipToBounds,
        Clip,
        OpacityMask,
        Effect,
        EffectProperty,
        CacheMode,
        RenderTransform,
        RenderTransformProperty,
        RenderTransformOrigin,
        Position
    }

    private static (CompositorCanvas, CountingControl) Create()
    {
        var services = new CompositorCanvas();
        var control = new CountingControl
        {
            Width = 40, Height = 30,
            RenderTransform = new TranslateTransform(),
            Effect = new BlurEffect { Radius = 1 },
            [Canvas.LeftProperty] = 10, [Canvas.TopProperty] = 20
        };
        services.Canvas.Children.Add(control);
        services.RunJobs();
        return (services, control);
    }

    // Reads the composition visual state that the change must update.
    private static object? Read(CompositionDrawListVisual comp, Change change) => change switch
    {
        Change.Opacity => comp.Opacity,
        Change.IsVisible => comp.Visible,
        Change.ClipToBounds => comp.ClipToBounds,
        Change.Clip => comp.Clip,
        Change.OpacityMask => comp.OpacityMask,
        Change.Effect or Change.EffectProperty => comp.Effect,
        Change.CacheMode => comp.CacheMode,
        Change.RenderTransform or Change.RenderTransformProperty or Change.RenderTransformOrigin
            => comp.TransformMatrix,
        Change.Position => comp.Offset,
        _ => throw new ArgumentOutOfRangeException(nameof(change))
    };

    private static void Apply(CountingControl control, Change change)
    {
        switch (change)
        {
            case Change.Opacity: control.Opacity = 0.5; break;
            case Change.IsVisible: control.IsVisible = false; break;
            case Change.ClipToBounds: control.ClipToBounds = !control.ClipToBounds; break;
            case Change.Clip: control.Clip = new RectangleGeometry(new Rect(0, 0, 10, 10)); break;
            case Change.OpacityMask: control.OpacityMask = Brushes.Black; break;
            case Change.Effect: control.Effect = new DropShadowEffect(); break;
            case Change.EffectProperty: ((BlurEffect)control.Effect!).Radius = 5; break;
            case Change.CacheMode: control.CacheMode = new BitmapCache(); break;
            case Change.RenderTransform: control.RenderTransform = new ScaleTransform(2, 2); break;
            case Change.RenderTransformProperty: ((TranslateTransform)control.RenderTransform!).X = 5; break;
            case Change.RenderTransformOrigin:
                control.RenderTransform = new ScaleTransform(2, 2);
                break;
            case Change.Position: Canvas.SetLeft(control, 50); break;
        }
    }

    [Theory]
    [InlineData(Change.Opacity)]
    [InlineData(Change.IsVisible)]
    [InlineData(Change.ClipToBounds)]
    [InlineData(Change.Clip)]
    [InlineData(Change.OpacityMask)]
    [InlineData(Change.Effect)]
    [InlineData(Change.EffectProperty)]
    [InlineData(Change.CacheMode)]
    [InlineData(Change.RenderTransform)]
    [InlineData(Change.RenderTransformProperty)]
    [InlineData(Change.RenderTransformOrigin)]
    [InlineData(Change.Position)]
    public void Composition_Property_Change_Updates_Composition_Visual_Without_Render(Change change)
    {
        var (services, control) = Create();
        using var _ = services;

        if (change == Change.RenderTransformOrigin)
        {
            // The origin has an effect only with a transform that is not a translation.
            Apply(control, change);
            services.RunJobs();
        }

        var comp = control.CompositionVisual!;
        var drawList = comp.DrawList;
        var renderCount = control.RenderCount;
        var before = Read(comp, change);

        if (change == Change.RenderTransformOrigin)
            control.RenderTransformOrigin = RelativePoint.TopLeft;
        else
            Apply(control, change);
        services.RunJobs();

        Assert.NotEqual(before, Read(comp, change));
        Assert.Equal(renderCount, control.RenderCount);
        Assert.Same(drawList, comp.DrawList);
    }

    [Theory]
    [InlineData(Change.IsVisible)]
    [InlineData(Change.ClipToBounds)]
    [InlineData(Change.Clip)]
    [InlineData(Change.OpacityMask)]
    [InlineData(Change.Effect)]
    [InlineData(Change.EffectProperty)]
    [InlineData(Change.CacheMode)]
    [InlineData(Change.RenderTransform)]
    [InlineData(Change.RenderTransformProperty)]
    [InlineData(Change.Position)]
    public void Composition_Property_Change_Keeps_Other_Composition_Properties(Change change)
    {
        var (services, control) = Create();
        using var _ = services;
        var comp = control.CompositionVisual!;

        // A value that the app set on the composition visual, for example for an implicit animation.
        comp.Opacity = 0.25f;
        Apply(control, change);
        services.RunJobs();

        Assert.Equal(0.25f, comp.Opacity);
    }

    [Fact]
    public void InvalidateVisual_Keeps_Composition_Properties()
    {
        var (services, control) = Create();
        using var _ = services;
        var comp = control.CompositionVisual!;
        var renderCount = control.RenderCount;

        comp.Opacity = 0.25f;
        comp.Offset = new Vector3D(1, 2, 0);
        control.InvalidateVisual();
        services.RunJobs();

        Assert.Equal(renderCount + 1, control.RenderCount);
        Assert.Equal(0.25f, comp.Opacity);
        Assert.Equal(new Vector3D(1, 2, 0), comp.Offset);
    }

    [Fact]
    public void Size_Change_Renders_Content()
    {
        var (services, control) = Create();
        using var _ = services;
        var comp = control.CompositionVisual!;
        var renderCount = control.RenderCount;

        control.Width = 60;
        services.RunJobs();

        Assert.Equal(new Vector(60, 30), comp.Size);
        Assert.Equal(renderCount + 1, control.RenderCount);
    }

    [Fact]
    public void InvalidateVisual_After_Composition_Property_Change_Renders_Content()
    {
        var (services, control) = Create();
        using var _ = services;
        var comp = control.CompositionVisual!;
        var renderCount = control.RenderCount;

        control.Opacity = 0.5;
        control.InvalidateVisual();
        services.RunJobs();

        Assert.Equal(0.5f, comp.Opacity);
        Assert.Equal(renderCount + 1, control.RenderCount);
    }
}
