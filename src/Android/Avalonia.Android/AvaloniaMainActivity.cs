using System;
using System.Runtime.Versioning;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.App;

namespace Avalonia.Android
{
    public class AvaloniaMainActivity : AppCompatActivity, IActivityResultHandler, IActivityNavigationService, IActivityConfigurationService
    {
        public Action<int, Result, Intent> ActivityResult { get; set; }
        public Action<int, string[], Permission[]> RequestPermissionsResult { get; set; }

        public event EventHandler<AndroidBackRequestedEventArgs> BackRequested;
        public event EventHandler ConfigurationChanged;

        public override void OnBackPressed()
        {
            var eventArgs = new AndroidBackRequestedEventArgs();

            BackRequested?.Invoke(this, eventArgs);

            if (!eventArgs.Handled)
            {
                base.OnBackPressed();
            }
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            ActivityResult?.Invoke(requestCode, resultCode, data);
        }

        [SupportedOSPlatform("android23.0")]
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            RequestPermissionsResult?.Invoke(requestCode, permissions, grantResults);
        }
    }

    public abstract partial class AvaloniaMainActivity<TApp> : AvaloniaMainActivity  where TApp : Application, new()
    {
        internal AvaloniaView View { get; set; }

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
            if (OperatingSystem.IsAndroidVersionAtLeast(28) && Window is { Attributes: { } attributes })
            {
                attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
        }

        protected override void OnDestroy()
        {
            View.Content = null;

            View.ViewTreeObserver?.RemoveOnGlobalLayoutListener(_listener);

            base.OnDestroy();
        }

        private class GlobalLayoutListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
        {
            private readonly AvaloniaView _view;

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
