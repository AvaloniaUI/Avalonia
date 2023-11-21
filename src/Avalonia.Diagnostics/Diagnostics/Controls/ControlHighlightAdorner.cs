using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Reactive;

namespace Avalonia.Diagnostics.Controls;

internal class ControlHighlightAdorner : Control
{

    readonly IPen _pen;

    private ControlHighlightAdorner(IPen pen)
    {
        _pen = pen;
        this.Clip = null;
    }

    public static IDisposable? Add(InputElement owner, IBrush highlightBrush)
    {

        if (AdornerLayer.GetAdornerLayer(owner) is { } layer)
        {
            var pen = new Pen(highlightBrush, 2).ToImmutable();
            var adorner = new ControlHighlightAdorner(pen)
            {
                [AdornerLayer.AdornedElementProperty] = owner
            };
            layer.Children.Add(adorner);

            return Disposable.Create((layer, adorner), state =>
            {
                state.layer.Children.Remove(state.adorner);
            });
        }
        return default;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(_pen, Bounds.Deflate(2));
    }

}
