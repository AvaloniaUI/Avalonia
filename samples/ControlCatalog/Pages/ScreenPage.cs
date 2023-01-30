using System;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public class ScreenPage : UserControl
    {
        private double _leftMost;
        private double _topMost;
        private IBrush _primaryBrush = SolidColorBrush.Parse("#FF0078D7");
        private IBrush _defaultBrush = Brushes.LightGray;
        private IPen _activePen = new Pen(Brushes.Black);
        private IPen _defaultPen = new Pen(Brushes.DarkGray);

        protected override bool BypassFlowDirectionPolicies => true;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if(VisualRoot is Window w)
            {
                w.PositionChanged += (_, _) => InvalidateVisual();
            }
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

            var activeScreen = w.Screens.ScreenFromBounds(new PixelRect(w.Position, PixelSize.FromSize(w.Bounds.Size, scaling)));
            double maxBottom = 0;

            for (int i = 0; i<screens.Count; i++ )
            {
                var screen = screens[i];

                if (screen.Bounds.X / 10f < _leftMost)
                {
                    _leftMost = screen.Bounds.X / 10f;
                    Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
                    return;
                }
                if (screen.Bounds.Y / 10f < _topMost)
                {
                    _topMost = screen.Bounds.Y / 10f;
                    Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
                    return;
                }
                bool primary = screen.IsPrimary;
                bool active = screen.Equals(activeScreen);

                Rect boundsRect = new Rect(screen.Bounds.X / 10f + Math.Abs(_leftMost), screen.Bounds.Y / 10f+Math.Abs(_topMost), screen.Bounds.Width / 10f,
                                  screen.Bounds.Height / 10f);
                Rect workingAreaRect = new Rect(screen.WorkingArea.X / 10f + Math.Abs(_leftMost), screen.WorkingArea.Y / 10f+Math.Abs(_topMost), screen.WorkingArea.Width / 10f,
                                   screen.WorkingArea.Height / 10f);

                context.DrawRectangle(primary ? _primaryBrush : _defaultBrush, active ? _activePen : _defaultPen, boundsRect);
                context.DrawRectangle(primary ? _primaryBrush : _defaultBrush, active ? _activePen : _defaultPen, workingAreaRect);

                var identifier = CreateScreenIdentifier((i+1).ToString(), primary);
                var center = boundsRect.Center - new Point(identifier.Width / 2.0f, identifier.Height / 2.0f);

                context.DrawText(identifier, center);
                maxBottom = Math.Max(maxBottom, boundsRect.Bottom);
            }

            double currentHeight = maxBottom;

            for(int i = 0; i< screens.Count; i++)
            {
                var screen = screens[i];

                var formattedText = CreateFormattedText($"Screen {i+1}", 18);
                context.DrawText(formattedText, new Point(0, currentHeight));
                currentHeight += 25;

                formattedText = CreateFormattedText($"Bounds: {screen.Bounds.Width}:{screen.Bounds.Height}");
                context.DrawText(formattedText, new Point(15, currentHeight));
                currentHeight += 20;

                formattedText = CreateFormattedText($"WorkArea: {screen.WorkingArea.Width}:{screen.WorkingArea.Height}");
                context.DrawText(formattedText, new Point(15, currentHeight));
                currentHeight += 20;

                formattedText = CreateFormattedText($"Scaling: {screen.Scaling * 100}%");
                context.DrawText(formattedText, new Point(15, currentHeight));
                currentHeight += 20;

                formattedText = CreateFormattedText($"IsPrimary: {screen.IsPrimary}");

                context.DrawText(formattedText, new Point(15, currentHeight));
                currentHeight += 20;

                formattedText = CreateFormattedText( $"Current: {screen.Equals(activeScreen)}");
                context.DrawText(formattedText, new Point(15, currentHeight));
                currentHeight += 30;

            }

            context.DrawRectangle(_activePen, new Rect(w.Position.X / 10f + Math.Abs(_leftMost), w.Position.Y / 10f+Math.Abs(_topMost), w.Bounds.Width / 10, w.Bounds.Height / 10));
        }

        private static FormattedText CreateFormattedText(string textToFormat, double size = 12)
        {
            return new FormattedText(textToFormat, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                Typeface.Default, size, Brushes.Green);
        }

        private static FormattedText CreateScreenIdentifier(string textToFormat, bool primary)
        {
            return new FormattedText(textToFormat, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 20, primary ? Brushes.White : Brushes.Black);
        }
    }
}
