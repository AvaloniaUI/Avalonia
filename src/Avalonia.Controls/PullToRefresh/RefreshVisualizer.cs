using System;
using System.Numerics;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.PullToRefresh;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;

namespace Avalonia.Controls
{
    public class RefreshVisualizer : ContentControl
    {
        private const int DefaultIndicatorSize = 24;
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
        private float _startingRotationAngle;
        private double _interactionRatio;
        private bool _played;
        private ScalarKeyFrameAnimation? _rotateAnimation;

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

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            this.ClipToBounds = false;

            _root = e.NameScope.Find<Grid>("PART_Root");

            if (_root != null)
            {
                _content = Content as Control ?? new PathIcon()
                {
                    Height = DefaultIndicatorSize,
                    Width = DefaultIndicatorSize,
                    Name = "PART_Icon",
                    Data = StreamGeometry.Parse(
                        "M18.6195264,3.31842271 C19.0080059,3.31842271 19.3290603,3.60710385 19.3798716,3.9816481 L19.3868766,4.08577298 L19.3868766,6.97963208 C19.3868766,7.36811161 19.0981955,7.68916605 18.7236513,7.73997735 L18.6195264,7.74698235 L15.7256673,7.74698235 C15.3018714,7.74698235 14.958317,7.40342793 14.958317,6.97963208 C14.958317,6.59115255 15.2469981,6.27009811 15.6215424,6.21928681 L15.7256673,6.21228181 L16.7044011,6.21182461 C13.7917384,3.87107476 9.52212532,4.05209336 6.81933829,6.75488039 C3.92253872,9.65167996 3.92253872,14.34832 6.81933829,17.2451196 C9.71613786,20.1419192 14.4127779,20.1419192 17.3095775,17.2451196 C19.0725398,15.4821573 19.8106555,12.9925923 19.3476248,10.58925 C19.2674502,10.173107 19.5398064,9.77076216 19.9559494,9.69058758 C20.3720923,9.610413 20.7744372,9.88276918 20.8546118,10.2989121 C21.4129973,13.1971899 20.5217103,16.2033812 18.3947747,18.3303168 C14.8986373,21.8264542 9.23027854,21.8264542 5.73414113,18.3303168 C2.23800371,14.8341794 2.23800371,9.16582064 5.73414113,5.66968323 C9.05475132,2.34907304 14.3349409,2.18235834 17.8523166,5.16953912 L17.8521761,4.08577298 C17.8521761,3.66197713 18.1957305,3.31842271 18.6195264,3.31842271 Z")
                };

                _content.Loaded += (s, e) =>
                {
                    var composition = ElementComposition.GetElementVisual(_content);

                    if (composition == null)
                        return;

                    var compositor = composition.Compositor;
                    composition.Opacity = 0;

                    var smoothRotationAnimation
                        = compositor.CreateScalarKeyFrameAnimation();
                    smoothRotationAnimation.Target = "RotationAngle";
                    smoothRotationAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue", new LinearEasing());
                    smoothRotationAnimation.Duration = TimeSpan.FromMilliseconds(100);

                    var opacityAnimation
                        = compositor.CreateScalarKeyFrameAnimation();
                    opacityAnimation.Target = "Opacity";
                    opacityAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue", new LinearEasing());
                    opacityAnimation.Duration = TimeSpan.FromMilliseconds(100);

                    var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
                    offsetAnimation.Target = "Offset";
                    offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue", new LinearEasing());
                    offsetAnimation.Duration = TimeSpan.FromMilliseconds(150);

                    var scaleAnimation
                        = compositor.CreateVector3KeyFrameAnimation();
                    scaleAnimation.Target = "Scale";
                    scaleAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue", new LinearEasing());
                    scaleAnimation.Duration = TimeSpan.FromMilliseconds(100);

                    var animation = compositor.CreateImplicitAnimationCollection();
                    animation["RotationAngle"] = smoothRotationAnimation;
                    animation["Offset"] = offsetAnimation;
                    animation["Scale"] = scaleAnimation;
                    animation["Opacity"] = opacityAnimation;

                    composition.ImplicitAnimations = animation;

                    UpdateContent();
                };

                if (_content != Content)
                    SetCurrentValue(ContentProperty, _content);
                else
                    RaisePropertyChanged(ContentProperty, null, Content, Data.BindingPriority.Style, true);

                OnOrientationChanged();

                UpdateContent();
            }
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
                            if(_rotateAnimation != null)
                            {
                                _rotateAnimation.IterationBehavior = AnimationIterationBehavior.Count;
                                _rotateAnimation = null;
                            }
                            contentVisual.Opacity = MinimumIndicatorOpacity;
                            contentVisual.RotationAngle = _startingRotationAngle;
                            visualizerVisual.Offset = IsPullDirectionVertical ?
                                new Vector3D(visualizerVisual.Offset.X, 0, 0) :
                                new Vector3D(0, visualizerVisual.Offset.Y, 0);
                            visual.Offset = visualizerVisual.Offset;
                            _content.InvalidateMeasure();
                            break;
                        case RefreshVisualizerState.Interacting:
                            _played = false;
                            contentVisual.Opacity = MinimumIndicatorOpacity;
                            contentVisual.RotationAngle = (float)(_startingRotationAngle + _interactionRatio * 2 * Math.PI);
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
                            contentVisual.RotationAngle = _startingRotationAngle + (float)(2 * Math.PI);
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
                                var scaleAnimation = contentVisual.Compositor!.CreateVector3KeyFrameAnimation();
                                scaleAnimation.Target = "Scale";
                                scaleAnimation.InsertKeyFrame(0.5f, new Vector3(1.5f, 1.5f, 1));
                                scaleAnimation.InsertKeyFrame(1f, new Vector3(1f, 1f, 1));
                                scaleAnimation.Duration = TimeSpan.FromSeconds(0.3);

                                contentVisual.StartAnimation("Scale", scaleAnimation);
                            }
                            break;
                        case RefreshVisualizerState.Refreshing:
                            _rotateAnimation = contentVisual.Compositor!.CreateScalarKeyFrameAnimation();
                            _rotateAnimation.Target = "RotationAngle";
                            _rotateAnimation.InsertKeyFrame(0, _startingRotationAngle, new LinearEasing());
                            _rotateAnimation.InsertKeyFrame(1, _startingRotationAngle + (float)(2 * Math.PI), new LinearEasing());
                            _rotateAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
                            _rotateAnimation.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;
                            _rotateAnimation.Duration = TimeSpan.FromSeconds(0.5);

                            contentVisual.StartAnimation("RotationAngle", _rotateAnimation);
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
                            contentVisual.RotationAngle = _startingRotationAngle;
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
                if (_root != null && _content != null)
                {
                    _root.Children.Insert(0, _content);
                    _content.VerticalAlignment = Layout.VerticalAlignment.Center;
                    _content.HorizontalAlignment = Layout.HorizontalAlignment.Center;
                }

                UpdateContent();
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
                            _startingRotationAngle = 0.0f;
                            break;
                        case PullDirection.LeftToRight:
                            _startingRotationAngle = (float)(-Math.PI / 2);
                            break;
                        case PullDirection.RightToLeft:
                            _startingRotationAngle = (float)(Math.PI / 2);
                            break;
                    }
                    break;
                case RefreshVisualizerOrientation.Normal:
                    _startingRotationAngle = 0.0f;
                    break;
                case RefreshVisualizerOrientation.Rotate90DegreesCounterclockwise:
                    _startingRotationAngle = (float)(Math.PI / 2);
                    break;
                case RefreshVisualizerOrientation.Rotate270DegreesCounterclockwise:
                    _startingRotationAngle = (float)(-Math.PI / 2);
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
