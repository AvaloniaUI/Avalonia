using System;
using System.Runtime.Versioning;
using Android.Content;
using Android.Content.Res;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using AndroidX.CustomView.Widget;
using Avalonia.Android.Platform;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Android
{
    public partial class AvaloniaView : FrameLayout
    {
        private EmbeddableControlRoot? _root;
        private ExploreByTouchHelper? _accessHelper;

        private readonly ViewImpl _view;

        private IDisposable? _timerSubscription;
        private object? _content;
        private bool _surfaceCreated;

        public AvaloniaView(Context context) : base(context)
        {
            _view = new ViewImpl(this);

            AddView(_view.View);

            this.SetBackgroundColor(global::Android.Graphics.Color.Transparent);

            _view.InternalView.SurfaceWindowCreated += InternalView_SurfaceWindowCreated;
        }

        private void InternalView_SurfaceWindowCreated(object? sender, EventArgs e)
        {
            _surfaceCreated = true;

            if (Visibility == ViewStates.Visible)
            {
                OnVisibilityChanged(true);
            }
        }

        internal TopLevelImpl TopLevelImpl => _view;
        internal TopLevel? TopLevel => _root;

        public object? Content
        {
            get { return _root?.Content; }
            set
            {
                _content = null;
                if (_root != null)
                    _root.Content = value;
                else
                {
                    _content = value;
                }
            }
        }

        internal new void Dispose()
        {
            _root?.Dispose();
            _root = null;
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            OnVisibilityChanged(false);
            _surfaceCreated = false;

            if(_accessHelper is { }  accessHelper)
            {
                ViewCompat.SetAccessibilityDelegate(this, null);
                _accessHelper = null;
            }
        }

        protected override void OnAttachedToWindow()
        {
            _root = new EmbeddableControlRoot(_view);
            _root.Prepare();
            if(_content != null)
            {
                _root.Content = _content;
            }

            _accessHelper = new AvaloniaAccessHelper(this);
            ViewCompat.SetAccessibilityDelegate(this, _accessHelper);
            SendConfigurationChanged(Context?.Resources?.Configuration);

            base.OnAttachedToWindow();
        }

        [SupportedOSPlatform("android24.0")]
        public override void OnVisibilityAggregated(bool isVisible)
        {
            base.OnVisibilityAggregated(isVisible);
            OnVisibilityChanged(isVisible);
        }

        protected override void OnVisibilityChanged(View changedView, [GeneratedEnum] ViewStates visibility)
        {
            base.OnVisibilityChanged(changedView, visibility);
            OnVisibilityChanged(visibility == ViewStates.Visible);
        }

        internal void OnVisibilityChanged(bool isVisible)
        {
            if (_root == null || !_surfaceCreated)
                return;

            if (isVisible && _timerSubscription == null)
            {
                if (AvaloniaLocator.Current.GetService<IRenderTimer>() is ChoreographerTimer timer)
                {
                    _timerSubscription = timer.SubscribeView(this);
                }

                _root.StartRendering();

                if (_view.TryGetFeature<IInsetsManager>(out var insetsManager) == true)
                {
                    (insetsManager as AndroidInsetsManager)?.ApplyStatusBarState();
                }
            }
            else if (!isVisible && _timerSubscription != null)
            {
                _root.StopRendering();
                _timerSubscription?.Dispose();
                _timerSubscription = null;
            }
        }
        
        protected override void OnConfigurationChanged(Configuration? newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            SendConfigurationChanged(newConfig ?? Context?.Resources?.Configuration);
        }

        private void SendConfigurationChanged(Configuration? newConfig)
        {
            _view.InsetsManager?.SetDefaultSystemLightMode(!(newConfig?.UiMode.HasFlag(UiMode.NightYes) ?? false));
            if (Context is { } context && newConfig is { } config)
            {
                var settings =
                    AvaloniaLocator.Current.GetRequiredService<IPlatformSettings>() as AndroidPlatformSettings;
                settings?.OnViewConfigurationChanged(context, config);
                ((AndroidScreens)_view.TryGetFeature<IScreenImpl>()!).OnChanged();
            }
        }

        class ViewImpl : TopLevelImpl
        {
            public ViewImpl(AvaloniaView avaloniaView) : base(avaloniaView)
            {
                View.FocusChange += ViewImpl_FocusChange;
            }

            private void ViewImpl_FocusChange(object? sender, FocusChangeEventArgs e)
            {
                if(!e.HasFocus)
                    LostFocus?.Invoke();
            }
        }
    }
}
