using System;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using AndroidX.Lifecycle;

namespace Avalonia.Android
{
    public abstract class AvaloniaMainActivity : AppCompatActivity, IActivityResultHandler
    {
        internal static object ViewContent;

        public Action<int, Result, Intent> ActivityResult { get; set; }
        internal AvaloniaView View;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            View = new AvaloniaView(this);
            if (ViewContent != null)
            {
                View.Content = ViewContent;
            }

            if (Avalonia.Application.Current.ApplicationLifetime is SingleViewLifetime lifetime)
            {
                lifetime.View = View;
            }

            base.OnCreate(savedInstanceState);

            SetContentView(View);
        }

        public object Content
        {
            get
            {
                return ViewContent;
            }
            set
            {
                ViewContent = value;
                if (View != null)
                    View.Content = value;
            }
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
        }

        protected override void OnDestroy()
        {
            View.Content = null;

            base.OnDestroy();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            ActivityResult?.Invoke(requestCode, resultCode, data);
        }
    }
}
