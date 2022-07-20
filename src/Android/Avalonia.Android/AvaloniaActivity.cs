using Android.OS;
using AndroidX.AppCompat.App;
using Android.Content.Res;
using AndroidX.Lifecycle;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Android.Runtime;
using Android.App;
using Android.Content;
using System;

namespace Avalonia.Android
{
    public abstract class AvaloniaActivity : AppCompatActivity
    {
        internal class SingleViewLifetime : ISingleViewApplicationLifetime
        {
            public AvaloniaView View { get; internal set; }

            public Control MainView
            {
                get => (Control)View.Content;
                set => View.Content = value;
            }
        }

        internal Action<int, Result, Intent> ActivityResult;
        internal AvaloniaView View;
        internal AvaloniaViewModel _viewModel;

        protected abstract AppBuilder CreateAppBuilder();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            var builder = CreateAppBuilder();


            var lifetime = new SingleViewLifetime();

            builder.AfterSetup(x =>
            {
                _viewModel = new ViewModelProvider(this).Get(Java.Lang.Class.FromType(typeof(AvaloniaViewModel))) as AvaloniaViewModel;

                View = new AvaloniaView(this);
                if (_viewModel.Content != null)
                {
                    View.Content = _viewModel.Content;
                }

                SetContentView(View);
                lifetime.View = View;

                View.Prepare();
            });

            builder.SetupWithLifetime(lifetime);

            base.OnCreate(savedInstanceState);
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

    public abstract class AvaloniaActivity<TApp> : AvaloniaActivity where TApp : Application, new()
    {
        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder.UseAndroid();

        protected override AppBuilder CreateAppBuilder()
        {
            var builder = AppBuilder.Configure<TApp>();

            return CustomizeAppBuilder(builder);
        }
    }
}
