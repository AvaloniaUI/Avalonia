#nullable enable
using System;
using System.Reactive.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    public class ManagedPointer : TemplatedControl
    {
        private class PointerTransform : ITransform
        {
            public Point Position { get; set; }

            public Matrix Value => Matrix.CreateTranslation(Position.X, Position.Y);
        }
        
        public static readonly StyledProperty<Cursor?> PointerCursorProperty =
            AvaloniaProperty.Register<ManagedPointer, Cursor?>(nameof(PointerCursor), null, true);

        public ManagedPointer(TopLevel visualRoot)
        {
            IsVisible = false;
            IsHitTestVisible = false;
            ZIndex = int.MaxValue;
            
            visualRoot.GetObservable(TopLevel.PointerOverElementProperty)
                .Select(
                    x => (x as InputElement)?.GetObservable(CursorProperty) ?? Observable.Empty<Cursor>())
                .Switch().Subscribe(cursor => PointerCursor = cursor);

            RenderTransform = new PointerTransform();

            var layer = OverlayLayer.GetOverlayLayer(visualRoot);

            ((ISetLogicalParent)this).SetParent(visualRoot);
            layer.Children.Add(this);

            InputManager.Instance.PreProcess.OfType<RawPointerEventArgs>().Subscribe(x =>
            {
                IsVisible = true;
                
                if (x is { Type: RawPointerEventType.Move } args &&
                    RenderTransform is PointerTransform t)
                {
                    t.Position = args.Position;
                    InvalidateVisual();
                }
            });

            InputManager.Instance.PreProcess.OfType<RawPointerEventArgs>()
                .Throttle(TimeSpan.FromSeconds(5))
                .ObserveOn(AvaloniaScheduler.Instance)
                .Subscribe(x => IsVisible = false);
        }

        public Cursor? PointerCursor
        {
            get => GetValue(PointerCursorProperty);
            set => SetValue(PointerCursorProperty, value);
        }
    }
}
