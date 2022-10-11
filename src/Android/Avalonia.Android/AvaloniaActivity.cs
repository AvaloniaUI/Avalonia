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
    public abstract class AvaloniaActivity : AppCompatActivity
    {
        internal Action<int, Result, Intent> ActivityResult;
        internal AvaloniaView View;
        internal AvaloniaViewModel _viewModel;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            _viewModel = new ViewModelProvider(this).Get(Java.Lang.Class.FromType(typeof(AvaloniaViewModel))) as AvaloniaViewModel;

            View = new AvaloniaView(this);
            if (_viewModel.Content != null)
            {
                View.Content = _viewModel.Content;
            }

            View.Prepare();

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
                return _viewModel.Content;
            }
            set
            {
                _viewModel.Content = value;
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
