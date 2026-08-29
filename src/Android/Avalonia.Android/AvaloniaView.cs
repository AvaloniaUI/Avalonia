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
        private readonly ViewImpl _view;
        private readonly ExploreByTouchHelper _accessHelper;

        private bool _isRendering;
        private bool _surfaceCreated;

        public AvaloniaView(Context context) : base(context)
        {
            _view = new ViewImpl(this);

            AddView(_view.View);

            _root = new EmbeddableControlRoot(_view);
            _root.Prepare();

            SetBackgroundColor(global::Android.Graphics.Color.Transparent);
            OnConfigurationChanged(null);

            _view.InternalView!.SurfaceWindowCreated += InternalView_SurfaceWindowCreated;
            _view.InternalView.SurfaceWindowDestroyed += InternalView_SurfaceWindowDestroyed;

            _accessHelper = new AvaloniaAccessHelper(this);
            ViewCompat.SetAccessibilityDelegate(this, _accessHelper);
        }

        private void InternalView_SurfaceWindowCreated(object? sender, EventArgs e)
        {
            _surfaceCreated = true;

            if (Visibility == ViewStates.Visible)
            {
                OnVisibilityChanged(true);

                _root?.InvalidateMeasure();
                Invalidate();
            }
        }

        private void InternalView_SurfaceWindowDestroyed(object? sender, EventArgs e)
        {
            OnVisibilityChanged(false);
            _surfaceCreated = false;
        }

        internal TopLevelImpl TopLevelImpl => _view;
        internal TopLevel? TopLevel => _root;

        public object? Content
        {
            get => _root?.Content;
            set => _root?.Content = value;
        }

        internal new void Dispose()
        {
            OnVisibilityChanged(false);
            _surfaceCreated = false;
            _root?.Dispose();
            _root = null;
        }

        protected override void OnAttachedToWindow()
        {
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

            if (isVisible && !_isRendering)
            {
                _isRendering = true;
                _root.StartRendering();

                if (_view.TryGetFeature<IInsetsManager>(out var insetsManager) == true)
                {
                    (insetsManager as AndroidInsetsManager)?.ApplyStatusBarState();
                }
            }
            else if (!isVisible && _isRendering)
            {
                _isRendering = false;
                _root.StopRendering();
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
            if (Context is not null && newConfig is not null)
            {
                ((AndroidScreens)_view.TryGetFeature<IScreenImpl>()!).OnChanged();
            }
        }

        class ViewImpl : TopLevelImpl
        {
            public ViewImpl(AvaloniaView avaloniaView) : base(avaloniaView)
            {
                View!.FocusChange += ViewImpl_FocusChange;
            }

            private void ViewImpl_FocusChange(object? sender, FocusChangeEventArgs e)
            {
                if (!e.HasFocus)
                    LostFocus?.Invoke();
            }
        }
    }
}
