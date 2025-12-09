using System;
using System.Numerics;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.PullToRefresh;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;

namespace Avalonia.Controls
{
    public class RefreshVisualizer : ContentControl
    {
        /// <summary>
        /// Provides calculated values for use with the <see cref="ProgressBar"/>'s control theme or template.
        /// </summary>
        /// <remarks>
        /// This class is NOT intended for general use outside of control templates.
        /// </remarks>
        public class RefreshVisualizerTemplateSettings : AvaloniaObject
        {
            private float _rotationStartAngle;
            private float _rotationEndAngle;

            private bool _isRotationAnimating;
            private bool _triggerScaleAnimation;

            /// <summary>
            /// Defines the <see cref="RotationStartAngle"/> property.
            /// </summary>
            public static readonly DirectProperty<RefreshVisualizerTemplateSettings, float> RotationStartAngleProperty =
                AvaloniaProperty.RegisterDirect<RefreshVisualizerTemplateSettings, float>(
                    nameof(RotationStartAngle),
                    p => p.RotationStartAngle,
                    (p, o) => p.RotationStartAngle = o);

            /// <summary>
            /// Defines the <see cref="RotationEndAngle"/> property.
            /// </summary>
            public static readonly DirectProperty<RefreshVisualizerTemplateSettings, float> RotationEndAngleProperty =
                AvaloniaProperty.RegisterDirect<RefreshVisualizerTemplateSettings, float>(
                    nameof(RotationEndAngle),
                    p => p.RotationEndAngle,
                    (p, o) => p.RotationEndAngle = o);

            /// <summary>
            /// Defines the <see cref="IsRotationAnimating"/> property.
            /// </summary>
            public static readonly DirectProperty<RefreshVisualizerTemplateSettings, bool> IsRotationAnimatingProperty =
                AvaloniaProperty.RegisterDirect<RefreshVisualizerTemplateSettings, bool>(
                    nameof(IsRotationAnimating),
                    p => p.IsRotationAnimating,
                    (p, o) => p.IsRotationAnimating = o);

            /// <summary>
            /// Defines the <see cref="TriggerScaleAnimation"/> property.
            /// </summary>
            public static readonly DirectProperty<RefreshVisualizerTemplateSettings, bool> TriggerScaleAnimationProperty =
                AvaloniaProperty.RegisterDirect<RefreshVisualizerTemplateSettings, bool>(
                    nameof(TriggerScaleAnimation),
                    p => p.TriggerScaleAnimation,
                    (p, o) => p.TriggerScaleAnimation = o);

            /// <summary>
            /// Used by themes to define the start angle of the indicator icon when animating
            /// </summary>
            public float RotationStartAngle
            {
                get => _rotationStartAngle;
                set => SetAndRaise(RotationStartAngleProperty, ref _rotationStartAngle, value);
            }

            /// <summary>
            /// Used by themes to define the end angle of the indicator icon when animating
            /// </summary>
            public float RotationEndAngle
            {
                get => _rotationEndAngle;
                set => SetAndRaise(RotationEndAngleProperty, ref _rotationEndAngle, value);
            }

            /// <summary>
            /// Used by themes to trigger animating the indicator icon
            /// </summary>
            public bool IsRotationAnimating
            {
                get => _isRotationAnimating;
                set => SetAndRaise(IsRotationAnimatingProperty, ref _isRotationAnimating, value);
            }

            /// <summary>
            /// Used by themes to trigger the scale animation of the indicator when the indicator state
            /// changes to Pending
            /// </summary>
            public bool TriggerScaleAnimation
            {
                get => _triggerScaleAnimation;
                set => SetAndRaise(TriggerScaleAnimationProperty, ref _triggerScaleAnimation, value);
            }

            /// <summary>
            /// Used by themes to define the initial scale value of the Pending scale animation
            /// </summary>
            public Vector3 ScaleStartValue { get; } = new Vector3(1.5f, 1.5f, 1);

            /// <summary>
            /// Used by themes to define the final scale value of the Pending scale animation
            /// </summary>
            public Vector3 ScaleEndValue { get; } = new Vector3(1f, 1f, 1);
        }

        private const float MinimumIndicatorOpacity = 0.4f;
        private const float ParallaxPositionRatio = 0.5f;
        private double _executingRatio = 0.8;

        private RefreshVisualizerState _refreshVisualizerState;
        private RefreshInfoProvider? _refreshInfoProvider;
        private IDisposable? _isInteractingSubscription;
        private IDisposable? _interactionRatioSubscription;
        private bool _isInteractingForRefresh;
        private Grid? _root;
        private Control? _content;
        private RefreshVisualizerOrientation _orientation;
        private double _interactionRatio;
        private bool _played;

        private bool IsPullDirectionVertical => PullDirection == PullDirection.TopToBottom || PullDirection == PullDirection.BottomToTop;
        private bool IsPullDirectionFar => PullDirection == PullDirection.BottomToTop || PullDirection == PullDirection.RightToLeft;

        /// <summary>
        /// Defines the <see cref="PullDirection"/> property.
        /// </summary>
        internal static readonly StyledProperty<PullDirection> PullDirectionProperty =
            AvaloniaProperty.Register<RefreshVisualizer, PullDirection>(nameof(PullDirection), PullDirection.TopToBottom);

        /// <summary>
        /// Defines the <see cref="RefreshRequested"/> event.
        /// </summary>
        public static readonly RoutedEvent<RefreshRequestedEventArgs> RefreshRequestedEvent =
            RoutedEvent.Register<RefreshVisualizer, RefreshRequestedEventArgs>(nameof(RefreshRequested), RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="RefreshVisualizerState"/> property.
        /// </summary>
        public static readonly DirectProperty<RefreshVisualizer, RefreshVisualizerState> RefreshVisualizerStateProperty =
            AvaloniaProperty.RegisterDirect<RefreshVisualizer, RefreshVisualizerState>(nameof(RefreshVisualizerState),
                s => s.RefreshVisualizerState);

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly DirectProperty<RefreshVisualizer, RefreshVisualizerOrientation> OrientationProperty =
            AvaloniaProperty.RegisterDirect<RefreshVisualizer, RefreshVisualizerOrientation>(nameof(Orientation),
                s => s.Orientation, (s, o) => s.Orientation = o);

        /// <summary>
        /// Defines the <see cref="RefreshInfoProvider"/> property.
        /// </summary>
        internal static readonly DirectProperty<RefreshVisualizer, RefreshInfoProvider?> RefreshInfoProviderProperty =
            AvaloniaProperty.RegisterDirect<RefreshVisualizer, RefreshInfoProvider?>(nameof(RefreshInfoProvider),
                s => s.RefreshInfoProvider, (s, o) => s.RefreshInfoProvider = o);

        /// <summary>
        /// Gets or sets a value that indicates the refresh state of the visualizer.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1032", Justification = "False positive")]
        protected RefreshVisualizerState RefreshVisualizerState
        {
            get => _refreshVisualizerState;
            private set
            {
                SetAndRaise(RefreshVisualizerStateProperty, ref _refreshVisualizerState, value);
                UpdateContent();
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates the orientation of the visualizer.
        /// </summary>
        public RefreshVisualizerOrientation Orientation
        {
            get => _orientation;
            set => SetAndRaise(OrientationProperty, ref _orientation, value);
        }

        internal PullDirection PullDirection
        {
            get => GetValue(PullDirectionProperty);
            set => SetValue(PullDirectionProperty, value);
        }

        internal RefreshInfoProvider? RefreshInfoProvider
        {
            get => _refreshInfoProvider; 
            set
            {
                if (_refreshInfoProvider != null)
                {
                    _refreshInfoProvider.RenderTransform = null;
                }
                SetAndRaise(RefreshInfoProviderProperty, ref _refreshInfoProvider, value);
            }
        }

        /// <summary>
        /// Occurs when an update of the content has been initiated.
        /// </summary>
        public event EventHandler<RefreshRequestedEventArgs>? RefreshRequested
        {
            add => AddHandler(RefreshRequestedEvent, value);
            remove => RemoveHandler(RefreshRequestedEvent, value);
        }

        /// <summary>
        /// Template settings for themes
        /// </summary>
        public RefreshVisualizerTemplateSettings TemplateSettings { get; } = new RefreshVisualizerTemplateSettings();

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            this.ClipToBounds = false;

            _root = e.NameScope.Find<Grid>("PART_Root");

            if (_root != null)
            {
                OnOrientationChanged();
                
                if (_root != null && _content != null)
                {
                    _root.Children.Insert(0, _content);
                    _content.VerticalAlignment = Layout.VerticalAlignment.Center;
                    _content.HorizontalAlignment = Layout.HorizontalAlignment.Center;

                    UpdateContent();
                }
            }
        }

        private void OnContentLoaded(object? s, RoutedEventArgs e)
        {
            if (_content == null)
                return;
            
            var composition = ElementComposition.GetElementVisual(_content);

            if (composition == null) return;
            composition.Opacity = 0;

            UpdateContent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            UpdateContent();
        }

        private void UpdateContent()
        {
            if (_content != null && _root != null)
            {
                var root = _root;
                var visual = _refreshInfoProvider?.Visual;
                var contentVisual = ElementComposition.GetElementVisual(_content);
                var visualizerVisual = ElementComposition.GetElementVisual(this);
                if (visual != null && contentVisual != null && visualizerVisual != null)
                {
                    contentVisual.CenterPoint = new Vector3D((_content.Bounds.Width / 2), (_content.Bounds.Height / 2), 0);
                    switch (RefreshVisualizerState)
                    {
                        case RefreshVisualizerState.Idle:
                            _played = false;
                            TemplateSettings.TriggerScaleAnimation = false;
                            TemplateSettings.IsRotationAnimating = false;
                            contentVisual.Opacity = MinimumIndicatorOpacity;
                            contentVisual.RotationAngle = TemplateSettings.RotationStartAngle;
                            contentVisual.Scale = new Vector3D(1, 1, 1);
                            visualizerVisual.Offset = IsPullDirectionVertical ?
                                new Vector3D(visualizerVisual.Offset.X, 0, 0) :
                                new Vector3D(0, visualizerVisual.Offset.Y, 0);
                            visual.Offset = visualizerVisual.Offset;
                            _content.InvalidateMeasure();
                            break;
                        case RefreshVisualizerState.Interacting:
                            _played = false;
                            TemplateSettings.TriggerScaleAnimation = false;
                            contentVisual.Opacity = MinimumIndicatorOpacity;
                            contentVisual.RotationAngle = (float)(TemplateSettings.RotationStartAngle + _interactionRatio * 2 * Math.PI);
                            Vector3D offset = default;
                            if (IsPullDirectionVertical)
                            {
                                offset = new Vector3D(0, (_interactionRatio * (IsPullDirectionFar ? -1 : 1) * root.Bounds.Height), 0);
                            }
                            else
                            {
                                offset = new Vector3D((_interactionRatio * (IsPullDirectionFar ? -1 : 1) * root.Bounds.Width), 0, 0);
                            }
                            visual.Offset = offset;
                            visualizerVisual.Offset = IsPullDirectionVertical ? 
                                new Vector3D(visualizerVisual.Offset.X, offset.Y, 0) :
                                new Vector3D(offset.X, visualizerVisual.Offset.Y, 0);
                            break;
                        case RefreshVisualizerState.Pending:
                            contentVisual.Opacity = 1;
                            contentVisual.RotationAngle = TemplateSettings.RotationStartAngle + (float)(2 * Math.PI);
                            if (IsPullDirectionVertical)
                            {
                                offset = new Vector3D(0, (_interactionRatio * (IsPullDirectionFar ? -1 : 1) * root.Bounds.Height), 0);
                            }
                            else
                            {
                                offset = new Vector3D((_interactionRatio * (IsPullDirectionFar ? -1 : 1) * root.Bounds.Width), 0, 0);
                            }
                            visual.Offset = offset;
                            visualizerVisual.Offset = IsPullDirectionVertical ? 
                                new Vector3D(visualizerVisual.Offset.X, offset.Y, 0) : 
                                new Vector3D(offset.X, visualizerVisual.Offset.Y, 0);

                            if (!_played)
                            {
                                _played = true;
                                TemplateSettings.TriggerScaleAnimation = true;
                            }
                            break;
                        case RefreshVisualizerState.Refreshing:
                            TemplateSettings.TriggerScaleAnimation = false;
                            TemplateSettings.RotationEndAngle = TemplateSettings.RotationStartAngle + (float)(2 * Math.PI);
                            TemplateSettings.IsRotationAnimating = true;
                            contentVisual.Opacity = 1;
                            float translationRatio = (float)(_refreshInfoProvider != null ?  (1.0f - _refreshInfoProvider.ExecutionRatio) * ParallaxPositionRatio : 1.0f) 
                                * (IsPullDirectionFar ? -1f : 1f);
                            if (IsPullDirectionVertical)
                            {
                                offset = new Vector3D(0, (_executingRatio * (IsPullDirectionFar ? -1 : 1) * root.Bounds.Height), 0);
                            }
                            else
                            {
                                offset = new Vector3D((_executingRatio * (IsPullDirectionFar ? -1 : 1) * root.Bounds.Width), 0, 0);
                            }
                            visual.Offset = offset;
                            contentVisual.Offset += IsPullDirectionVertical ? new Vector3D(0, (translationRatio * root.Bounds.Height), 0) :
                                new Vector3D((translationRatio * root.Bounds.Width), 0, 0);
                            visualizerVisual.Offset = IsPullDirectionVertical ?
                                new Vector3D(visualizerVisual.Offset.X, offset.Y, 0) :
                                new Vector3D(offset.X, visualizerVisual.Offset.Y, 0);
                            break;
                        case RefreshVisualizerState.Peeking:
                            contentVisual.Opacity = 1;
                            contentVisual.RotationAngle = TemplateSettings.RotationStartAngle;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Initiates an update of the content.
        /// </summary>
        public void RequestRefresh()
        {
            RefreshVisualizerState = RefreshVisualizerState.Refreshing;
            RefreshInfoProvider?.OnRefreshStarted();

            RaiseRefreshRequested();
        }

        private void RefreshCompleted()
        {
            RefreshVisualizerState = RefreshVisualizerState.Idle;

            RefreshInfoProvider?.OnRefreshCompleted();
        }

        private void RaiseRefreshRequested()
        {
            var refreshArgs = new RefreshRequestedEventArgs(RefreshCompleted, RefreshRequestedEvent);

            refreshArgs.IncrementCount();

            RaiseEvent(refreshArgs);

            refreshArgs.DecrementCount();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == RefreshInfoProviderProperty)
            {
                OnRefreshInfoProviderChanged();
            }
            else if (change.Property == ContentProperty)
            {
                if (change.OldValue is Control c)
                {
                    c.Loaded -= OnContentLoaded;
                    _root?.Children.Remove(c);
                }

                _content = change.NewValue as Control;

                if (_content != null)
                {
                    _content.Loaded += OnContentLoaded;
                }

                if (_root != null && _content != null)
                {
                    _root.Children.Insert(0, _content);
                    _content.VerticalAlignment = Layout.VerticalAlignment.Center;
                    _content.HorizontalAlignment = Layout.HorizontalAlignment.Center;

                    UpdateContent();
                }
            }
            else if (change.Property == OrientationProperty)
            {
                OnOrientationChanged();

                UpdateContent();
            }
            else if (change.Property == BoundsProperty)
            {
                switch (PullDirection)
                {
                    case PullDirection.TopToBottom:
                        RenderTransform = new TranslateTransform(0, -Bounds.Height);
                        break;
                    case PullDirection.BottomToTop:
                        RenderTransform = new TranslateTransform(0, Bounds.Height);
                        break;
                    case PullDirection.LeftToRight:
                        RenderTransform = new TranslateTransform(-Bounds.Width, 0);
                        break;
                    case PullDirection.RightToLeft:
                        RenderTransform = new TranslateTransform(Bounds.Width, 0);
                        break;
                }

                UpdateContent();
            }
            else if(change.Property == PullDirectionProperty)
            {
                OnOrientationChanged();

                UpdateContent();
            }
        }

        private void OnOrientationChanged()
        {
            switch (_orientation)
            {
                case RefreshVisualizerOrientation.Auto:
                    switch (PullDirection)
                    {
                        case PullDirection.TopToBottom:
                        case PullDirection.BottomToTop:
                            TemplateSettings.RotationStartAngle = 0.0f;
                            break;
                        case PullDirection.LeftToRight:
                            TemplateSettings.RotationStartAngle = (float)(-Math.PI / 2);
                            break;
                        case PullDirection.RightToLeft:
                            TemplateSettings.RotationStartAngle = (float)(Math.PI / 2);
                            break;
                    }
                    break;
                case RefreshVisualizerOrientation.Normal:
                    TemplateSettings.RotationStartAngle = 0.0f;
                    break;
                case RefreshVisualizerOrientation.Rotate90DegreesCounterclockwise:
                    TemplateSettings.RotationStartAngle = (float)(Math.PI / 2);
                    break;
                case RefreshVisualizerOrientation.Rotate270DegreesCounterclockwise:
                    TemplateSettings.RotationStartAngle = (float)(-Math.PI / 2);
                    break;
            }
        }

        private void OnRefreshInfoProviderChanged()
        {
            _isInteractingSubscription?.Dispose();
            _isInteractingSubscription = null;
            _interactionRatioSubscription?.Dispose();
            _interactionRatioSubscription = null;

            if (RefreshInfoProvider != null)
            {
                _isInteractingSubscription = RefreshInfoProvider.GetObservable(RefreshInfoProvider.IsInteractingForRefreshProperty)
                    .Subscribe(InteractingForRefreshObserver);

                _interactionRatioSubscription = RefreshInfoProvider.GetObservable(RefreshInfoProvider.InteractionRatioProperty)
                    .Subscribe(InteractionRatioObserver);

                _executingRatio = RefreshInfoProvider.ExecutionRatio;
            }
            else
            {
                _executingRatio = 1;
            }
        }

        private void InteractionRatioObserver(double obj)
        {
            var wasAtZero = _interactionRatio == 0.0;
            _interactionRatio = obj;

            if (_isInteractingForRefresh)
            {
                if (RefreshVisualizerState == RefreshVisualizerState.Idle)
                {
                    if (wasAtZero)
                    {
                        if (_interactionRatio > _executingRatio)
                        {
                            RefreshVisualizerState = RefreshVisualizerState.Pending;
                        }
                        else if (_interactionRatio > 0)
                        {
                            RefreshVisualizerState = RefreshVisualizerState.Interacting;
                        }
                    }
                    else if (_interactionRatio > 0)
                    {
                        RefreshVisualizerState = RefreshVisualizerState.Peeking;
                    }
                }
                else if (RefreshVisualizerState == RefreshVisualizerState.Interacting)
                {
                    if (_interactionRatio <= 0)
                    {
                        RefreshVisualizerState = RefreshVisualizerState.Idle;
                    }
                    else if (_interactionRatio > _executingRatio)
                    {
                        RefreshVisualizerState = RefreshVisualizerState.Pending;
                    }
                    else
                    {
                        UpdateContent();
                    }
                }
                else if (RefreshVisualizerState == RefreshVisualizerState.Pending)
                {
                    if (_interactionRatio <= _executingRatio)
                    {
                        RefreshVisualizerState = RefreshVisualizerState.Interacting;
                    }
                    else if (_interactionRatio <= 0)
                    {
                        RefreshVisualizerState = RefreshVisualizerState.Idle;
                    }
                    else
                    {
                        UpdateContent();
                    }
                }
            }
            else
            {
                if (RefreshVisualizerState != RefreshVisualizerState.Refreshing)
                {
                    if (_interactionRatio > 0)
                    {
                        RefreshVisualizerState = RefreshVisualizerState.Peeking;
                    }
                    else
                    {
                        RefreshVisualizerState = RefreshVisualizerState.Idle;
                    }
                }
            }
        }

        private void InteractingForRefreshObserver(bool obj)
        {
            _isInteractingForRefresh = obj;

            if (!_isInteractingForRefresh)
            {
                switch (_refreshVisualizerState)
                {
                    case RefreshVisualizerState.Pending:
                        RequestRefresh();
                        break;
                    case RefreshVisualizerState.Refreshing:
                        // We don't want to interrupt a currently executing refresh.
                        break;
                    default:
                        RefreshVisualizerState = RefreshVisualizerState.Idle;
                        break;
                }
            }
        }
    }
}
