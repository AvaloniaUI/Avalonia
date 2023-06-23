using System;
using System.Diagnostics;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;

namespace Avalonia.Android
{
    public class AvaloniaMainActivity : AppCompatActivity, IActivityResultHandler, IActivityNavigationService
    {
        public Action<int, Result, Intent> ActivityResult { get; set; }
        public Action<int, string[], Permission[]> RequestPermissionsResult { get; set; }

        public event EventHandler<AndroidBackRequestedEventArgs> BackRequested;

        public override void OnBackPressed()
        {
            var eventArgs = new AndroidBackRequestedEventArgs();

            BackRequested?.Invoke(this, eventArgs);

            if (!eventArgs.Handled)
            {
                base.OnBackPressed();
            }
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            ActivityResult?.Invoke(requestCode, resultCode, data);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            RequestPermissionsResult?.Invoke(requestCode, permissions, grantResults);
        }
    }

    public abstract partial class AvaloniaMainActivity<TApp> : AvaloniaMainActivity  where TApp : Application, new()
    {
        internal AvaloniaView View;
        private GlobalLayoutListener _listener;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            InitializeApp();

            base.OnCreate(savedInstanceState);

            SetContentView(View);

            _listener = new GlobalLayoutListener(View);

            View.ViewTreeObserver?.AddOnGlobalLayoutListener(_listener);
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Android only respects LayoutInDisplayCutoutMode value if it has been set once before window becomes visible.
            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
        }

        protected override void OnDestroy()
        {
            View.Content = null;

            View.ViewTreeObserver?.RemoveOnGlobalLayoutListener(_listener);

            base.OnDestroy();
        }

        class GlobalLayoutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
        {
            private AvaloniaView _view;

            public GlobalLayoutListener(AvaloniaView view)
            {
                _view = view;
            }

            public void OnGlobalLayout()
            {
                _view.TopLevelImpl?.Resize(_view.TopLevelImpl.ClientSize);
            }
        }
    }
}
