using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Platform;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control that automatically adjusts its child position or height based on the input pane's height to ensure the content is visible.
    /// </summary>
    public class InputPaneAwareView : Decorator
    {
        private InputPaneAwareBehavior _behavior;
        private IInputPane? _inputPane;
        private bool _firstLayoutDone;

        /// <summary>
        /// Defines the <see cref="Behavior"/> property.
        /// </summary>
        public static readonly DirectProperty<InputPaneAwareView, InputPaneAwareBehavior> BehaviorProperty = AvaloniaProperty.RegisterDirect<InputPaneAwareView, InputPaneAwareBehavior>(
            nameof(Behavior),
            o => o.Behavior,
            (o, x) => o.Behavior = x);

        private static readonly StyledProperty<double> CurrentInputPanePaddingProperty = AvaloniaProperty.Register<InputPaneAwareView, double>(
            nameof(CurrentInputPanePadding), coerce: (o, x) =>
            {
                return x;
            });

        /// <summary>
        /// Gets or sets how the view reacts to the input pane state. 
        /// </summary>
        public InputPaneAwareBehavior Behavior
        {
            get => _behavior;
            set => SetAndRaise(BehaviorProperty, ref _behavior, value);
        }

        private double CurrentInputPanePadding
        {
            get => GetValue(CurrentInputPanePaddingProperty);
            set => SetValue(CurrentInputPanePaddingProperty, value);
        }

        static InputPaneAwareView()
        {
            AffectsMeasure<InputPaneAwareView>(BehaviorProperty, CurrentInputPanePaddingProperty);
            AffectsArrange<InputPaneAwareView>(CurrentInputPanePaddingProperty);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_inputPane != null && _firstLayoutDone && VisualRoot is { } root && Behavior == InputPaneAwareBehavior.Resize)
            {
                SetCurrentValue(PaddingProperty, new Thickness(0, 0, 0, CurrentInputPanePadding));
            }
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _firstLayoutDone = true;

            if (Behavior == InputPaneAwareBehavior.Pan)
            {
                Child?.Arrange(new Rect(new Point(0, -CurrentInputPanePadding), finalSize));

                return finalSize;
            }
            else
                return base.ArrangeOverride(finalSize);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _inputPane = TopLevel.GetTopLevel(this)?.InputPane;
            _inputPane?.StateChanged += InputPaneAwareView_StateChanged;
            EnsureInputPanePaddingApplied();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _inputPane?.StateChanged -= InputPaneAwareView_StateChanged;
            _inputPane = null;
            SetCurrentValue(PaddingProperty, default);
#pragma warning disable AVP1012 // An AvaloniaObject should use SetCurrentValue when assigning its own StyledProperty or AttachedProperty values
            CurrentInputPanePadding = 0;
#pragma warning restore AVP1012 // An AvaloniaObject should use SetCurrentValue when assigning its own StyledProperty or AttachedProperty values
        }

        private void EnsureInputPanePaddingApplied()
        {
            SetCurrentValue(PaddingProperty, default);

            if (_inputPane != null && _firstLayoutDone && this.VisualRoot is { } root && Behavior != InputPaneAwareBehavior.None)
            {
                var occludedRect = _inputPane.OccludedRect;

                var transformMatrix = this.TransformToVisual(root) ?? Matrix.Identity;

                var translatedRect = Bounds.TransformToAABB(transformMatrix);

                var intersect = occludedRect.Intersect(translatedRect);

#pragma warning disable AVP1012 // An AvaloniaObject should use SetCurrentValue when assigning its own StyledProperty or AttachedProperty values
                CurrentInputPanePadding = intersect.Height;
            }
            else
            {
                CurrentInputPanePadding = 0;
            }
#pragma warning restore AVP1012 // An AvaloniaObject should use SetCurrentValue when assigning its own StyledProperty or AttachedProperty values
        }

        private void InputPaneAwareView_StateChanged(object? sender, InputPaneStateEventArgs e)
        {
            var transition = new DoubleTransition()
            {
                Property = CurrentInputPanePaddingProperty,
                Duration = e.AnimationDuration,
                Easing = e.Easing as Easing ?? new LinearEasing()
            };

            Transitions =
            [
                transition
            ];

            EnsureInputPanePaddingApplied();

            InvalidateMeasure();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if(change.Property == BehaviorProperty)
            {
                EnsureInputPanePaddingApplied();
            }
        }
    }
}
