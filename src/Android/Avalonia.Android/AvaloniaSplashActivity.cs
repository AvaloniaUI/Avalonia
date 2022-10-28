using Android.OS;
using AndroidX.AppCompat.App;
using AndroidX.Lifecycle;

namespace Avalonia.Android
{
    public abstract class AvaloniaSplashActivity : AppCompatActivity
    {
        protected abstract AppBuilder CreateAppBuilder();

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var builder = CreateAppBuilder();

            var lifetime = new SingleViewLifetime();

            builder.SetupWithLifetime(lifetime);
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
