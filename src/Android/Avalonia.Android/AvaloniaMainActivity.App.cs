namespace Avalonia.Android;

public class AvaloniaMainActivity<TApp> : AvaloniaMainActivity
    where TApp : Application, new()
{
    protected override AppBuilder CreateAppBuilder() => AppBuilder.Configure<TApp>().UseAndroid();
}
