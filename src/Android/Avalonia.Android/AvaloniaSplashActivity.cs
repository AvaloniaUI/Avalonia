using Android.OS;
using AndroidX.AppCompat.App;

namespace Avalonia.Android
{
    public abstract class AvaloniaSplashActivity : AppCompatActivity
    {
        protected abstract AppBuilder CreateAppBuilder();

        private static AppBuilder s_appBuilder;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (s_appBuilder == null)
            {
                var builder = CreateAppBuilder();

                var lifetime = new SingleViewLifetime();

                builder.SetupWithLifetime(lifetime);

                s_appBuilder = builder;
            }
        }
    }

    public abstract class AvaloniaSplashActivity<TApp> : AvaloniaSplashActivity where TApp : Application, new()
    {
        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder.UseAndroid();

        protected override AppBuilder CreateAppBuilder()
        {
            var builder = AppBuilder.Configure<TApp>();

            return CustomizeAppBuilder(builder);
        }
    }
}
