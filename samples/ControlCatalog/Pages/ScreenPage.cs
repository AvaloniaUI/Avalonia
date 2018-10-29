using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;

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
            Window w = (Window)VisualRoot;
            Screen[] screens = w.Screens.All;

            Pen p = new Pen(Brushes.Black);
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
                    
                    FormattedText text = new FormattedText()
                    {
                        Typeface = Typeface.Default
                    };

                    text.Text = $"Bounds: {screen.Bounds.Width}:{screen.Bounds.Height}";
                    context.DrawText(Brushes.Black, boundsRect.Position.WithY(boundsRect.Size.Height), text);
                    
                    text.Text = $"WorkArea: {screen.WorkingArea.Width}:{screen.WorkingArea.Height}";
                    context.DrawText(Brushes.Black, boundsRect.Position.WithY(boundsRect.Size.Height + 20), text);
                    
                    text.Text = $"Primary: {screen.Primary}";
                    context.DrawText(Brushes.Black, boundsRect.Position.WithY(boundsRect.Size.Height + 40), text);
                    
                    text.Text = $"Current: {screen.Equals(w.Screens.ScreenFromBounds(new Rect(w.Position, w.Bounds.Size)))}";
                    context.DrawText(Brushes.Black, boundsRect.Position.WithY(boundsRect.Size.Height + 60), text);
                }

            context.DrawRectangle(p, new Rect(w.Position.X / 10f + Math.Abs(_leftMost), w.Position.Y / 10, w.Bounds.Width / 10, w.Bounds.Height / 10));
        }
    }
}
