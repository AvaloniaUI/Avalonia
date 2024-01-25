using System;
using System.Runtime.Versioning;
using Android.Content;
using Android.Content.Res;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Avalonia.Android.Platform;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Android
{
    public class AvaloniaView : FrameLayout
    {
        private EmbeddableControlRoot _root;
        private readonly ViewImpl _view;

        private IDisposable _timerSubscription;

        public AvaloniaView(Context context) : base(context)
        {
            _view = new ViewImpl(this);
            AddView(_view.View);

            _root = new EmbeddableControlRoot(_view);
            _root.Prepare();

            this.SetBackgroundColor(global::Android.Graphics.Color.Transparent);
            OnConfigurationChanged();
        }

        internal TopLevelImpl TopLevelImpl => _view;

        public object Content
        {
            get { return _root.Content; }
            set { _root.Content = value; }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _root?.Dispose();
            _root = null;
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            return _view.View.DispatchKeyEvent(e);
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

        private void OnVisibilityChanged(bool isVisible)
        {
            if (isVisible)
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
            else
            {
                _root.StopRendering();
                _timerSubscription?.Dispose();
            }
        }
        
        protected override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            OnConfigurationChanged();
        }

        private void OnConfigurationChanged()
        {
            var settings = AvaloniaLocator.Current.GetRequiredService<IPlatformSettings>() as AndroidPlatformSettings;
            settings?.OnViewConfigurationChanged(Context);
        }

        class ViewImpl : TopLevelImpl
        {
            public ViewImpl(AvaloniaView avaloniaView) : base(avaloniaView)
            {
                View.Focusable = true;
                View.FocusChange += ViewImpl_FocusChange;
            }

            private void ViewImpl_FocusChange(object sender, FocusChangeEventArgs e)
            {
                if(!e.HasFocus)
                    LostFocus?.Invoke();
            }

            protected override void OnResized(Size size)
            {
                MaxClientSize = size;
                base.OnResized(size);
            }

            public WindowState WindowState { get; set; }
            public IDisposable ShowDialog() => null;
        }
    }
}
