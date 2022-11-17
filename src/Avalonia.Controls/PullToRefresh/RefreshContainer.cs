using System;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.PullToRefresh;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    public class RefreshContainer : ContentControl
    {
        internal const int DefaultPullDimensionSize = 100;

        private readonly bool _hasDefaultRefreshInfoProviderAdapter;

        private ScrollViewerIRefreshInfoProviderAdapter? _refreshInfoProviderAdapter;
        private RefreshInfoProvider? _refreshInfoProvider;
        private IDisposable? _visualizerSizeSubscription;
        private Grid? _visualizerPresenter;
        private RefreshVisualizer? _refreshVisualizer;

        public static readonly RoutedEvent<RefreshRequestedEventArgs> RefreshRequestedEvent =
            RoutedEvent.Register<RefreshContainer, RefreshRequestedEventArgs>(nameof(RefreshRequested), RoutingStrategies.Bubble);

        internal static readonly DirectProperty<RefreshContainer, ScrollViewerIRefreshInfoProviderAdapter?> RefreshInfoProviderAdapterProperty =
            AvaloniaProperty.RegisterDirect<RefreshContainer, ScrollViewerIRefreshInfoProviderAdapter?>(nameof(RefreshInfoProviderAdapter),
                (s) => s.RefreshInfoProviderAdapter, (s, o) => s.RefreshInfoProviderAdapter = o);

        public static readonly DirectProperty<RefreshContainer, RefreshVisualizer?> RefreshVisualizerProperty =
            AvaloniaProperty.RegisterDirect<RefreshContainer, RefreshVisualizer?>(nameof(RefreshVisualizer),
                s => s.RefreshVisualizer, (s, o) => s.RefreshVisualizer = o);

        public static readonly StyledProperty<PullDirection> PullDirectionProperty =
            AvaloniaProperty.Register<RefreshContainer, PullDirection>(nameof(PullDirection), PullDirection.TopToBottom);

        public ScrollViewerIRefreshInfoProviderAdapter? RefreshInfoProviderAdapter
        {
            get => _refreshInfoProviderAdapter; set
            {
                SetAndRaise(RefreshInfoProviderAdapterProperty, ref _refreshInfoProviderAdapter, value);
            }
        }

        private bool _hasDefaultRefreshVisualizer;

        public RefreshVisualizer? RefreshVisualizer
        {
            get => _refreshVisualizer; set
            {
                if (_refreshVisualizer != null)
                {
                    _visualizerSizeSubscription?.Dispose();
                    _refreshVisualizer.RefreshRequested -= Visualizer_RefreshRequested;
                }

                SetAndRaise(RefreshVisualizerProperty, ref _refreshVisualizer, value);
            }
        }

        public PullDirection PullDirection
        {
            get => GetValue(PullDirectionProperty);
            set => SetValue(PullDirectionProperty, value);
        }

        public event EventHandler<RefreshRequestedEventArgs>? RefreshRequested
        {
            add => AddHandler(RefreshRequestedEvent, value);
            remove => RemoveHandler(RefreshRequestedEvent, value);
        }

        public RefreshContainer()
        {
            _hasDefaultRefreshInfoProviderAdapter = true;
            RefreshInfoProviderAdapter = new ScrollViewerIRefreshInfoProviderAdapter(PullDirection);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _visualizerPresenter = e.NameScope.Find<Grid>("PART_RefreshVisualizerPresenter");

            if (_refreshVisualizer == null)
            {
                _hasDefaultRefreshVisualizer = true;
                RefreshVisualizer = new RefreshVisualizer();
            }
            else
            {
                _hasDefaultRefreshVisualizer = false;
                RaisePropertyChanged(RefreshVisualizerProperty, default, _refreshVisualizer);
            }

            OnPullDirectionChanged();
        }

        private void OnVisualizerSizeChanged(Rect obj)
        {
            if (_hasDefaultRefreshInfoProviderAdapter)
            {
                RefreshInfoProviderAdapter = new ScrollViewerIRefreshInfoProviderAdapter(PullDirection);
            }
        }

        private void Visualizer_RefreshRequested(object? sender, RefreshRequestedEventArgs e)
        {
            var ev = new RefreshRequestedEventArgs(e.GetRefreshCompletionDeferral(), RefreshRequestedEvent);
            RaiseEvent(ev);
            ev.DecrementCount();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == RefreshInfoProviderAdapterProperty)
            {
                if (_refreshVisualizer != null)
                {
                    if (_refreshInfoProvider != null)
                    {
                        _refreshVisualizer.RefreshInfoProvider = _refreshInfoProvider;
                    }
                    else
                    {
                        if (RefreshInfoProviderAdapter != null && _refreshVisualizer != null)
                        {
                            _refreshInfoProvider = RefreshInfoProviderAdapter?.AdaptFromTree(this, _refreshVisualizer.Bounds.Size);

                            if (_refreshInfoProvider != null)
                            {
                                _refreshVisualizer.RefreshInfoProvider = _refreshInfoProvider;
                                RefreshInfoProviderAdapter?.SetAnimations(_refreshVisualizer);
                            }
                        }
                    }
                }
            }
            else if (change.Property == RefreshVisualizerProperty)
            {
                if (_visualizerPresenter != null)
                {
                    _visualizerPresenter.Children.Clear();
                    if (_refreshVisualizer != null)
                    {
                        _visualizerPresenter.Children.Add(_refreshVisualizer);
                    }
                }

                if (_refreshVisualizer != null)
                {
                    _refreshVisualizer.RefreshRequested += Visualizer_RefreshRequested;
                    _visualizerSizeSubscription = _refreshVisualizer.GetObservable(Control.BoundsProperty).Subscribe(OnVisualizerSizeChanged);
                }
            }
            else if (change.Property == PullDirectionProperty)
            {
                OnPullDirectionChanged();
            }
        }

        private void OnPullDirectionChanged()
        {
            if (_visualizerPresenter != null && _refreshVisualizer != null)
            {
                switch (PullDirection)
                {
                    case PullDirection.TopToBottom:
                        _visualizerPresenter.VerticalAlignment = Layout.VerticalAlignment.Top;
                        _visualizerPresenter.HorizontalAlignment = Layout.HorizontalAlignment.Stretch;
                        if (_hasDefaultRefreshVisualizer)
                        {
                            _refreshVisualizer.PullDirection = PullDirection.TopToBottom;
                            _refreshVisualizer.Height = DefaultPullDimensionSize;
                            _refreshVisualizer.Width = double.NaN;
                        }
                        break;
                    case PullDirection.BottomToTop:
                        _visualizerPresenter.VerticalAlignment = Layout.VerticalAlignment.Bottom;
                        _visualizerPresenter.HorizontalAlignment = Layout.HorizontalAlignment.Stretch;
                        if (_hasDefaultRefreshVisualizer)
                        {
                            _refreshVisualizer.PullDirection = PullDirection.BottomToTop;
                            _refreshVisualizer.Height = DefaultPullDimensionSize;
                            _refreshVisualizer.Width = double.NaN;
                        }
                        break;
                    case PullDirection.LeftToRight:
                        _visualizerPresenter.VerticalAlignment = Layout.VerticalAlignment.Stretch;
                        _visualizerPresenter.HorizontalAlignment = Layout.HorizontalAlignment.Left;
                        if (_hasDefaultRefreshVisualizer)
                        {
                            _refreshVisualizer.PullDirection = PullDirection.LeftToRight;
                            _refreshVisualizer.Width = DefaultPullDimensionSize;
                            _refreshVisualizer.Height = double.NaN;
                        }
                        break;
                    case PullDirection.RightToLeft:
                        _visualizerPresenter.VerticalAlignment = Layout.VerticalAlignment.Stretch;
                        _visualizerPresenter.HorizontalAlignment = Layout.HorizontalAlignment.Right;
                        if (_hasDefaultRefreshVisualizer)
                        {
                            _refreshVisualizer.PullDirection = PullDirection.RightToLeft;
                            _refreshVisualizer.Width = DefaultPullDimensionSize;
                            _refreshVisualizer.Height = double.NaN;
                        }
                        break;
                }

                if (_hasDefaultRefreshInfoProviderAdapter &&
                    _hasDefaultRefreshVisualizer &&
                    _refreshVisualizer.Bounds.Height == DefaultPullDimensionSize &&
                    _refreshVisualizer.Bounds.Width == DefaultPullDimensionSize)
                {
                    RefreshInfoProviderAdapter = new ScrollViewerIRefreshInfoProviderAdapter(PullDirection);
                }
            }
        }

        public void RequestRefresh()
        {
            _refreshVisualizer?.RequestRefresh();
        }
    }
}
