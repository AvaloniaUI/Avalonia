using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Diagnostics.SourceNavigator;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.SourceInfo;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.DesignerSupport.DesignerSelection
{
    internal class WindowInspectorState
    {
        private static readonly Color _selectionColorFill = Color.Parse("#330078D7");
        private static readonly Color _selectionColorStroke = Color.Parse("#0078D7");
        private static readonly Color _toggleButtonInactiveColor = Color.FromArgb(128, 30, 30, 30);

        private static readonly IBrush _selectionBrushStroke = new SolidColorBrush(_selectionColorStroke);
        private static readonly IBrush _toggleButtonInactiveBrush = new SolidColorBrush(_toggleButtonInactiveColor);

        private const double BoundsEpsilon = 2.0;

        public WindowInspectorState(Control window)
        {
            Window = window;
        }

        private Control Window { get; init; }
        private Panel? OverlayLayer { get; set; }
        private Rectangle? HighlightRect { get; set; }
        private Border? InfoPopup { get; set; }
        private TextBlock? InfoText { get; set; }
        private Border? ToggleButton { get; set; }
        private Point? ToggleButtonDragStart = null;

        private Visual? LastSelectedVisual { get; set; }
        private List<(Control control, SourceInfo info)>? CurrentAncestry { get; set; }
        private int SelectedAncestorIndex { get; set; }

        internal void Register()
        {
            CreateOverlay();
            Window.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
            Window.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
            Window.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        }

        internal void Unregister()
        {
            Window.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            Window.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
            Window.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged);

            if(ToggleButton != null)
            {
                ToggleButton.PointerPressed -= OnToggleButton_PointerPressed;
                ToggleButton.PointerMoved -= OnToggleButton_PointerMoved;
            }
        }

        internal void ClearOverlay()
        {
            InfoPopup?.SetValue(Visual.IsVisibleProperty, false);
            HighlightRect?.SetValue(Visual.IsVisibleProperty, false);
        }

        private void CreateOverlay()
        {
            OverlayLayer = TryGetAdornerLayer(Window);
            if (OverlayLayer == null)
                return;

            HighlightRect = new Rectangle
            {
                StrokeThickness = 2,
                IsVisible = false,
                IsHitTestVisible = false,
                Stroke = new SolidColorBrush(_selectionColorStroke),
                Fill = new SolidColorBrush(_selectionColorFill)
            };
            OverlayLayer.Children.Add(HighlightRect);

            InfoPopup = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 30, 30, 30)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6),
                IsVisible = false,
                Child = (InfoText = new TextBlock
                {
                    Foreground = Brushes.White,
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap
                })
            };
            OverlayLayer.Children.Add(InfoPopup);

            CreateToggleButton();
        }

        private void CreateToggleButton()
        {
            var buttonText = new TextBlock
            {
                Text = "🖱",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White,
            };

            var btn = new Border
            {
                Background = ElementInspector.IsDesignerSelecting
                    ? _selectionBrushStroke
                    : _toggleButtonInactiveBrush,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Width = 28,
                Height = 28,
                Child = buttonText,
                ZIndex = 9999
            };
           
            Canvas.SetRight(btn, 10);
            Canvas.SetTop(btn, 10);
            OverlayLayer?.Children.Add(btn);
            ToggleButton = btn;
            ToggleButton.PointerPressed += OnToggleButton_PointerPressed;
            ToggleButton.PointerMoved += OnToggleButton_PointerMoved;
        }

        internal void CreateInfoPopupDialogText()
        {
            if (InfoPopup == null || InfoText == null)
                return;

            InfoText.Inlines?.Clear();

            void AddLine(string label, string text, bool isCurrent = false)
            {
                var line = new Avalonia.Controls.Documents.Run($"{label} {text}\n")
                {
                    FontWeight = isCurrent ? FontWeight.Bold : FontWeight.Normal,
                    Foreground = isCurrent ? Brushes.White : new SolidColorBrush(Color.FromRgb(200, 200, 200))
                };
                InfoText?.Inlines?.Add(line);
            }

            // Parent (above current)
            if (CurrentAncestry != null && SelectedAncestorIndex < CurrentAncestry.Count - 1)
            {
                var parent = CurrentAncestry[SelectedAncestorIndex + 1];
                if (parent.info != default)
                    AddLine("Parent:", $"{System.IO.Path.GetFileName(parent.info.FilePath)}:{parent.info.Line}");
                else
                    AddLine("Parent:", parent.control.GetType().Name);
            }

            // Current element (bold)
            if (CurrentAncestry != null && SelectedAncestorIndex < CurrentAncestry.Count)
            {
                var current = CurrentAncestry[SelectedAncestorIndex];
                if (current.info != default)
                    AddLine("→:", $"{System.IO.Path.GetFileName(current.info.FilePath)}:{current.info.Line}", true);
                else
                    AddLine("→:", current.control.GetType().Name, true);
            }

            // Child (below current)
            if (SelectedAncestorIndex > 0)
            {
                var child = CurrentAncestry![SelectedAncestorIndex - 1];
                if (child.info != default)
                    AddLine("Child:", $"{System.IO.Path.GetFileName(child.info.FilePath)}:{child.info.Line}");
                else
                    AddLine("Child:", child.control.GetType().Name);
            }
            InfoPopup.IsVisible = true;
        }


        // --- Pointer handling per window ---
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (!ElementInspector.IsDesignerSelecting)
                return;

            var pos = e.GetPosition(Window);
            var visual = Window.GetVisualAt(pos, v => v != HighlightRect && v != InfoPopup);
            if (visual == ToggleButton || visual == ToggleButton?.Child)
                visual = null;

            var control = FindNearestControl(visual);
            if (control == null)
            {
                HighlightRect!.IsVisible = InfoPopup!.IsVisible = false;
                LastSelectedVisual = visual;
                return;
            }

            if (CurrentAncestry == null || LastSelectedVisual != visual)
            {
                CurrentAncestry = CollectAncestry(control);
                SelectedAncestorIndex = FindBestSelection(CurrentAncestry);
                UpdateSelection(e);
            }
            else
            {
                UpdateInfoPopup(e.GetPosition(Window));
            }
            LastSelectedVisual = visual;
        }

        private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!ElementInspector.IsDesignerSelecting ||
                Window == null ||
                CurrentAncestry == null)
                return;

            var pos = e.GetPosition(Window);
            var visual = Window.GetVisualAt(pos, v => v != HighlightRect && v != InfoPopup);

            // Ignore the toggle button itself
            if (visual == ToggleButton || visual == ToggleButton?.Child)
                visual = null;

            var control = FindNearestControl(visual);
            if (control == null)
                return;

            e.Handled = true;

            // Detect modifier keys for ancestor navigation
            var mods = e.KeyModifiers;
            if (mods.HasFlag(KeyModifiers.Shift))
            {
                if (SelectedAncestorIndex < CurrentAncestry.Count - 1)
                {
                    SelectedAncestorIndex++;
                    UpdateSelection(e);
                }
            }
            else if (mods.HasFlag(KeyModifiers.Control))
            {
                if (SelectedAncestorIndex > 0)
                {
                    SelectedAncestorIndex--;
                    UpdateSelection(e);
                }
            }
            else
            {
                await SourceNavigatorRegistry.NavigateToAsync(CurrentAncestry[SelectedAncestorIndex].control);
            }
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (!ElementInspector.IsDesignerSelecting ||
                CurrentAncestry == null ||
                CurrentAncestry.Count == 0)
                return;

            if (e.Delta.Y > 0 && SelectedAncestorIndex < CurrentAncestry.Count - 1)
                SelectedAncestorIndex++;
            else if (e.Delta.Y < 0 && SelectedAncestorIndex > 0)
                SelectedAncestorIndex--;

            UpdateSelection(e);
            e.Handled = true;
        }

        //toggle button
        private void OnToggleButton_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if(ToggleButton != null)
            {
                ElementInspector.SetDesignerSelecting(!ElementInspector.IsDesignerSelecting);

                ToggleButton.Background = ElementInspector.IsDesignerSelecting
                    ? _selectionBrushStroke
                    : _toggleButtonInactiveBrush;

                ToggleButtonDragStart = e.GetPosition(OverlayLayer);
            }
        }

        private void OnToggleButton_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (ToggleButton != null && ToggleButtonDragStart is { } start && e.GetCurrentPoint(ToggleButton).Properties.IsLeftButtonPressed)
            {
                var pos = e.GetPosition(OverlayLayer);
                Canvas.SetRight(ToggleButton, double.NaN);
                Canvas.SetLeft(ToggleButton, pos.X - ToggleButton.Width / 2);
                Canvas.SetTop(ToggleButton, pos.Y - ToggleButton.Height / 2);
            }
        }

        private void Highlight(Control control)
        {
            if (Window == null || OverlayLayer == null || HighlightRect == null)
                return;

            var topLeft = control.TranslatePoint(new Point(0, 0), Window);
            if (topLeft == null)
                return;

            var bounds = control.Bounds;
            var offset = OverlayLayer.TranslatePoint(new Point(0, 0), Window) ?? new Point();

            Canvas.SetLeft(HighlightRect, topLeft.Value.X - offset.X);
            Canvas.SetTop(HighlightRect, topLeft.Value.Y - offset.Y);
            HighlightRect.Width = bounds.Width;
            HighlightRect.Height = bounds.Height;
            HighlightRect.IsVisible = true;
        }

        private void UpdateInfoPopup(Point mousePos)
        {
            if (InfoPopup == null || Window == null)
                return;

            InfoPopup.Measure(Size.Infinity);
            var popupSize = InfoPopup.DesiredSize;
            var winBounds = Window.Bounds;

            double x = Math.Min(mousePos.X + 10, winBounds.Width - popupSize.Width - 5);
            double y = Math.Min(mousePos.Y + 10, winBounds.Height - popupSize.Height - 5);

            InfoPopup.RenderTransform = new TranslateTransform(Math.Max(0, x), Math.Max(0, y));
        }

        private void UpdateSelection(PointerEventArgs e)
        {
            if (CurrentAncestry == null)
                return;

            var current = CurrentAncestry[SelectedAncestorIndex];
            Highlight(current.control);
            UpdateInfoPopup(e.GetPosition(Window));
            CreateInfoPopupDialogText();
        }

        private static Panel? TryGetAdornerLayer(Control window)
        {
            var adorner = AdornerLayer.GetAdornerLayer(window);
            if (adorner != null)
            {
                return adorner;
            }
            else if (TopLevel.GetTopLevel(window) is { } tl)
            {
                var layers = tl.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault();
                return layers?.AdornerLayer;
            }
            return null;
        }

        private static Control? FindNearestControl(Visual? v)
        {
            while (v != null)
            {
                if (v is Control c)
                    return c;
                v = v.GetVisualParent();
            }
            return null;
        }

        private static List<(Control control, SourceInfo info)> CollectAncestry(Control start)
        {
            var ancestry = new List<(Control control, SourceInfo info)>();

            // Collect all parents (including self) that have SourceInfo
            Visual? current = start;
            while (current != null)
            {
                if (current is Control control)
                {
                    var info = Avalonia.Markup.Xaml.SourceInfo.Source.GetSourceInfo(control);
                    if (info != default)
                    {
                        ancestry.Add((control, info));
                    }
                }

                current = current.GetVisualParent();
            }

            if (ancestry.Count == 0)
                return new List<(Control control, SourceInfo info)> { (start, default) };

            return ancestry;
        }

        private static int FindBestSelection(List<(Control control, SourceInfo info)> ancestry)
        {
            // Compute bounds-based smart selection
            int bestIndex = ancestry.Count - 1;
            var lastBounds = ancestry[0].control.Bounds;

            // --- Phase 1: move upward until bounds change significantly ---
            for (int i = 1; i < ancestry.Count; i++)
            {
                var pb = ancestry[i].control.Bounds;
                bool boundsDiff =
                    Math.Abs(pb.Width - lastBounds.Width) > BoundsEpsilon ||
                    Math.Abs(pb.Height - lastBounds.Height) > BoundsEpsilon;

                if (boundsDiff)
                {
                    bestIndex = i; // this one has a different bounding box
                    break;
                }

                lastBounds = pb;
            }

            // --- Phase 2: move downward until we find a non-panel ---
            // (but don't go below index 0)
            while (bestIndex > 0)
            {
                var candidate = ancestry[bestIndex].control;
                if (candidate is Panel)
                    bestIndex--;
                else
                    break;
            }

            // If we went too far, fallback
            if (bestIndex < 0 || bestIndex >= ancestry.Count)
                bestIndex = 0;

            // Set the selected ancestor index globally for scrolling
            return bestIndex;
        }
    }

    /// <summary>
    /// Provides a simple design-mode element inspector overlay using AdornerLayer.
    /// </summary>
    internal static class ElementInspector
    {
        private static readonly Dictionary<Control, WindowInspectorState> _windows = new();
        private static bool _firstLoaded = true;
        internal static bool DevToolsOpened = false;

        public static bool IsDesignerSelecting { get; set; }

        // --- Initialization per window ---
        public static void Initialize(Control control)
        {
            if (_firstLoaded)
            {
                SourceNavigatorRegistry.RegisterIfNotExists(() => new VisualStudioSourceNavigator());
                SourceNavigatorRegistry.RegisterIfNotExists(() => new RiderSourceNavigator());
                SourceNavigatorRegistry.RegisterIfNotExists(() => new VsCodeSourceNavigator());
                _firstLoaded = false;
                SetDesignerSelecting(Design.IsDesignMode);
            }

            if (control is Window w)
                w.Opened += (_, _) => Register(w);
            else
                control.AttachedToVisualTree += (_, _) => Register(control);
        }

        private static void Register(Control control)
        {
            var root = control.GetVisualRoot() as Control ?? control;
            if (_windows.ContainsKey(root))
                return;

            var state = new WindowInspectorState(root);
            _windows[root] = state;

            state.Register();

            if (root is Window w)
                w.Closing += (_, _) => Unregister(root);
            else
                root.DetachedFromVisualTree += (_, _) => Unregister(root);
        }

        private static void Unregister(Control control)
        {
            if (_windows.Remove(control, out var state))
            {
                state.Unregister();
            }
        }

        internal static void SetDesignerSelecting(bool value)
        {
            ElementInspector.IsDesignerSelecting = value;
            foreach (var s in _windows.Values)
            {
                if (!value)
                {
                    s.ClearOverlay();
                }
            }
        }
    }
}

