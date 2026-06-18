using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls.Primitives;
using Avalonia.Rendering.Composition;

namespace ControlCatalog.Pages
{
    public partial class GestureSwipePage : UserControl
    {
        private bool _isInit;
        private CompositionVisual? _indicatorVisual;
        private Vector3D _swipeDelta;

        public GestureSwipePage()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (_isInit)
            {
                return;
            }

            _isInit = true;

            SwipePanel.AddHandler(InputElement.SwipeGestureEvent, OnSwipeGesture);
            SwipePanel.AddHandler(InputElement.SwipeGestureEndedEvent, OnSwipeGestureEnded);
        }

        private void EnsureVisual()
        {
            if (_indicatorVisual != null)
            {
                return;
            }

            _indicatorVisual = ElementComposition.GetElementVisual(SwipeIndicator);

            if (_indicatorVisual != null)
            {
                var offsetAnimation = _indicatorVisual.Compositor.CreateVector3KeyFrameAnimation();
                offsetAnimation.Target = "Offset";
                offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
                offsetAnimation.Duration = TimeSpan.FromMilliseconds(150);

                var implicitAnimations = _indicatorVisual.Compositor.CreateImplicitAnimationCollection();
                implicitAnimations["Offset"] = offsetAnimation;

                _indicatorVisual.ImplicitAnimations = implicitAnimations;
            }
        }

        private void OnSwipeGesture(object? sender, SwipeGestureEventArgs e)
        {
            EnsureVisual();

            if (_indicatorVisual != null)
            {
                _swipeDelta += new Vector3D(-e.Delta.X, -e.Delta.Y, 0);
                _indicatorVisual.Offset += new Vector3D(-e.Delta.X, -e.Delta.Y, 0);
            }

            DirectionText.Text = $"Direction: {e.SwipeDirection}";
            DeltaText.Text = $"Delta: {e.Delta.X:F1}, {e.Delta.Y:F1}";
            VelocityText.Text = $"Velocity: {e.Velocity.X:F0}, {e.Velocity.Y:F0}";
            SwipeDirectionLabel.Text = e.SwipeDirection.ToString();
            SwipeDirectionLabel.Opacity = 1;

            e.Handled = true;
        }

        private void OnSwipeGestureEnded(object? sender, SwipeGestureEndedEventArgs e)
        {
            if (_indicatorVisual != null)
            {
                _indicatorVisual.Offset -= _swipeDelta;
            }

            _swipeDelta = default;

            VelocityText.Text = $"Velocity: {e.Velocity.X:F0}, {e.Velocity.Y:F0} (ended)";
            SwipeDirectionLabel.Text = "Swipe here";
            SwipeDirectionLabel.Opacity = 0.5;
        }

        private void OnDirectionChanged(object? sender, RoutedEventArgs e)
        {
            if (SwipeRecognizer == null)
            {
                return;
            }

            SwipeRecognizer.CanHorizontallySwipe = HorizontalCheck.IsChecked == true;
            SwipeRecognizer.CanVerticallySwipe = VerticalCheck.IsChecked == true;
        }

        private void OnMouseEnabledChanged(object? sender, RoutedEventArgs e)
        {
            if (SwipeRecognizer == null)
            {
                return;
            }

            SwipeRecognizer.IsMouseEnabled = MouseCheck.IsChecked == true;
        }

        private void OnThresholdChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            if (SwipeRecognizer == null || ThresholdText == null)
            {
                return;
            }

            SwipeRecognizer.Threshold = e.NewValue;
            ThresholdText.Text = $"{e.NewValue:F0}";
        }
    }
}
