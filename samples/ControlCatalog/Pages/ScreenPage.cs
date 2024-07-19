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

        public ScreenPage()
        {
            var button = new Button();
            button.Content = "Request ScreenDetails";
            button.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
            button.Click += async (sender, args) =>
            {
                var success = TopLevel.GetTopLevel(this)!.Screens is { } screens ?
                    await screens.RequestScreenDetails() :
                    false;
                button.Content = "Request ScreenDetails: " + (success ? "Granted" : "Denied");
            };
            Content = button;
        }

        protected override bool BypassFlowDirectionPolicies => true;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel is Window w)
            {
                w.PositionChanged += (_, _) => InvalidateVisual();
            }

            if (topLevel?.Screens is { } screens)
            {
                screens.Changed += (_, _) =>
                {
                    Console.WriteLine("Screens Changed");
                    InvalidateVisual();
                };
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            double beginOffset = (Content as Visual)?.Bounds.Height + 10 ?? 0;

            var topLevel = TopLevel.GetTopLevel(this)!;
            if (topLevel.Screens is not { } screens)
            {
                var formattedText = CreateFormattedText("Current platform doesn't support Screens API.");
                context.DrawText(formattedText, new Point(15, 15 + beginOffset));
                return;
            }

            var activeScreen = screens.ScreenFromTopLevel(topLevel);
            double maxBottom = 0;

            for (int i = 0; i<screens.ScreenCount; i++ )
            {
                var screen = screens.All[i];

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

                Rect boundsRect = new Rect(screen.Bounds.X / 10f + Math.Abs(_leftMost), screen.Bounds.Y / 10f+Math.Abs(_topMost) + beginOffset, screen.Bounds.Width / 10f,
                                  screen.Bounds.Height / 10f);
                Rect workingAreaRect = new Rect(screen.WorkingArea.X / 10f + Math.Abs(_leftMost), screen.WorkingArea.Y / 10f+Math.Abs(_topMost) + beginOffset, screen.WorkingArea.Width / 10f,
                                   screen.WorkingArea.Height / 10f);

                context.DrawRectangle(primary ? _primaryBrush : _defaultBrush, active ? _activePen : _defaultPen, boundsRect);
                context.DrawRectangle(primary ? _primaryBrush : _defaultBrush, active ? _activePen : _defaultPen, workingAreaRect);
 
                var identifier = CreateScreenIdentifier((i+1).ToString(), primary);
                var center = boundsRect.Center - new Point(identifier.Width / 2.0f, identifier.Height / 2.0f + beginOffset);

                context.DrawText(identifier, center);
                maxBottom = Math.Max(maxBottom, boundsRect.Bottom);
            }

            double currentHeight = maxBottom;

            for(int i = 0; i< screens.ScreenCount; i++)
            {
                var screen = screens.All[i];

                var formattedText = CreateFormattedText($"Screen {i+1}", 18);
                context.DrawText(formattedText, new Point(0, currentHeight));
                currentHeight += 25;

                formattedText = CreateFormattedText($"DisplayName: {screen.DisplayName}");
                context.DrawText(formattedText, new Point(15, currentHeight));
                currentHeight += 20;
                
                formattedText = CreateFormattedText($"Handle: {screen.TryGetPlatformHandle()}");
                context.DrawText(formattedText, new Point(15, currentHeight));
                currentHeight += 20;

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
                
                formattedText = CreateFormattedText($"CurrentOrientation: {screen.CurrentOrientation}");
                context.DrawText(formattedText, new Point(15, currentHeight));
                currentHeight += 20;

                formattedText = CreateFormattedText( $"Current: {screen.Equals(activeScreen)}");
                context.DrawText(formattedText, new Point(15, currentHeight));
                currentHeight += 30;
            }

            if (topLevel is Window w)
            {
                var wPos = w.Position;
                var wSize = PixelSize.FromSize(w.FrameSize ?? w.ClientSize, w.DesktopScaling);
                context.DrawRectangle(_activePen,
                    new Rect(wPos.X / 10f + Math.Abs(_leftMost), wPos.Y / 10f + Math.Abs(_topMost) + beginOffset,
                        wSize.Width / 10d, wSize.Height / 10d));
            }
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
