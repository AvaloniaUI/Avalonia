using System;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Rendering;

namespace Avalonia.Android
{
    public class AvaloniaView : FrameLayout
    {
        private readonly EmbeddableControlRoot _root;
        private readonly ViewImpl _view;

        private IDisposable? _timerSubscription;

        public AvaloniaView(Context context) : base(context)
        {
            _view = new ViewImpl(context);
            AddView(_view.View);
            _root = new EmbeddableControlRoot(_view);
            _root.Prepare();
        }

        public object Content
        {
            get { return _root.Content; }
            set { _root.Content = value; }
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            return _view.View.DispatchKeyEvent(e);
        }

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

                _root.Renderer.Start();
            }
            else
            {
                _root.Renderer.Stop();
                _timerSubscription?.Dispose();
            }
        }

        class ViewImpl : TopLevelImpl
        {
            public ViewImpl(Context context) : base(context)
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
