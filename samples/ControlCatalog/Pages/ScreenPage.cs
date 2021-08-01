using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace ControlCatalog.Pages
{
    public class ScreenPage : UserControl
    {
        private double _leftMost;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Window w = (Window)VisualRoot;
            w.PositionChanged += (sender, args) => InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (!(VisualRoot is Window w))
            {
                return;                
            }
            var screens = w.Screens.All;
            var scaling = ((IRenderRoot)w).RenderScaling;

            var drawBrush = Brushes.Black;
            Pen p = new Pen(drawBrush);
            if (screens != null)
                foreach (Screen screen in screens)
                {
                    if (screen.Bounds.X / 10f < _leftMost)
                    {
                        _leftMost = screen.Bounds.X / 10f;
                        InvalidateVisual();
                        return;
                    }

                    Rect boundsRect = new Rect(screen.Bounds.X / 10f + Math.Abs(_leftMost), screen.Bounds.Y / 10f, screen.Bounds.Width / 10f,
                                      screen.Bounds.Height / 10f);
                    Rect workingAreaRect = new Rect(screen.WorkingArea.X / 10f + Math.Abs(_leftMost), screen.WorkingArea.Y / 10f, screen.WorkingArea.Width / 10f,
                                       screen.WorkingArea.Height / 10f);
                    
                    context.DrawRectangle(p, boundsRect);
                    context.DrawRectangle(p, workingAreaRect);

                    var text = new FormattedText() { Typeface = new Typeface("Arial"), FontSize = 18 };

                    text.Text = $"Bounds: {screen.Bounds.TopLeft} {screen.Bounds.Width}:{screen.Bounds.Height}";
                    context.DrawText(drawBrush, boundsRect.Position.WithY(boundsRect.Size.Height), text);
                    
                    text.Text = $"WorkArea: {screen.WorkingArea.TopLeft} {screen.WorkingArea.Width}:{screen.WorkingArea.Height}";
                    context.DrawText(drawBrush, boundsRect.Position.WithY(boundsRect.Size.Height + 20), text);

                    text.Text = $"Scaling: {screen.PixelDensity * 100}%";
                    context.DrawText(drawBrush, boundsRect.Position.WithY(boundsRect.Size.Height + 40), text);
                    
                    text.Text = $"Primary: {screen.Primary}";
                    context.DrawText(drawBrush, boundsRect.Position.WithY(boundsRect.Size.Height + 60), text);
                    
                    text.Text = $"Current: {screen.Equals(w.Screens.ScreenFromBounds(new PixelRect(w.Position, PixelSize.FromSize(w.Bounds.Size, scaling))))}";
                    context.DrawText(drawBrush, boundsRect.Position.WithY(boundsRect.Size.Height + 80), text);
                }

            context.DrawRectangle(p, new Rect(w.Position.X / 10f + Math.Abs(_leftMost), w.Position.Y / 10f, w.Bounds.Width / 10, w.Bounds.Height / 10));
        }
    }
}
