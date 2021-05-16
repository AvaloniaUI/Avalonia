using System;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;

namespace Avalonia.Controls
{
    public class ManagedPointer : TemplatedControl
    {
        private class PointerTransform : ITransform
        {
            public Point Position { get; set; }

            public Matrix Value => Matrix.CreateTranslation(Position.X, Position.Y);
        }

        public ManagedPointer(TopLevel visualRoot)
        {
            RenderTransform = new PointerTransform();

            var layer = OverlayLayer.GetOverlayLayer(visualRoot);

            ((ISetLogicalParent)this).SetParent(visualRoot);
            layer.Children.Add(this);

            InputManager.Instance.PreProcess.Subscribe(x =>
            {
                if (x is RawPointerEventArgs { Type: RawPointerEventType.Move } args &&
                    RenderTransform is PointerTransform t)
                {
                    t.Position = args.Position;
                    InvalidateVisual();
                }
            });
        }
    }
}
