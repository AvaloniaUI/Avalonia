using Android.OS;
using AndroidX.AppCompat.App;
using Android.Content.Res;
using AndroidX.Lifecycle;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;

namespace Avalonia.Android
{
    public abstract class AvaloniaActivity<TApp> : AppCompatActivity where TApp : Application, new()
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

        internal AvaloniaView View;
        internal AvaloniaViewModel _viewModel;

        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder.UseAndroid();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            var builder = AppBuilder.Configure<TApp>();
            
            CustomizeAppBuilder(builder);

            View = new AvaloniaView(this);
            SetContentView(View);

            var lifetime = new SingleViewLifetime();
            lifetime.View = View;

            builder.AfterSetup(x =>
            {
                _viewModel = new ViewModelProvider(this).Get(Java.Lang.Class.FromType(typeof(AvaloniaViewModel))) as AvaloniaViewModel;

                if (_viewModel.Content != null)
                {
                    View.Content = _viewModel.Content;
                }

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
    }
}
