#nullable enable

namespace Avalonia.Android
{
    partial class AvaloniaMainActivity<TApp> where TApp : Application, new()
    {
        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder.UseAndroid();

        private static AppBuilder? s_appBuilder;
        internal static object? ViewContent;

        public object? Content
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

        protected AppBuilder CreateAppBuilder()
        {
            var builder = AppBuilder.Configure<TApp>();

            return CustomizeAppBuilder(builder);
        }

        private void InitializeApp()
        {
            if (s_appBuilder == null)
            {
                var builder = CreateAppBuilder();

                builder.SetupWithLifetime(new SingleViewLifetime(this));

                s_appBuilder = builder;
            }

            View = new AvaloniaView(this);
            if (ViewContent != null)
            {
                View.Content = ViewContent;
            }

            if (Avalonia.Application.Current?.ApplicationLifetime is SingleViewLifetime lifetime)
            {
                lifetime.View = View;
            }
        }
    }
}
