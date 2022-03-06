using Android.OS;
using AndroidX.AppCompat.App;
using Android.Content.Res;
using AndroidX.Lifecycle;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;

namespace Avalonia.Android
{
    public abstract class AvaloniaActivity : AppCompatActivity
    {
        class SingleViewLifetime : ISingleViewApplicationLifetime
        {
            public SingleViewLifetime(AvaloniaView view)
            {
                View = view;
            }

            public AvaloniaView View;

            public Control MainView
            {
                get => (Control)View.Content;
                set => View.Content = value;
            }
        }

        internal AvaloniaView View;
        internal AvaloniaViewModel _viewModel;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            View = new AvaloniaView(this);
            SetContentView(View);

            _viewModel = new ViewModelProvider(this).Get(Java.Lang.Class.FromType(typeof(AvaloniaViewModel))) as AvaloniaViewModel;

            if (_viewModel.Content != null)
            {
                View.Content = _viewModel.Content;
            }

            AvaloniaLocator.CurrentMutable.Bind<ISingleViewApplicationLifetime>().ToConstant(new SingleViewLifetime(View));

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
