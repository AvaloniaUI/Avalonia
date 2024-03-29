#nullable enable

using Android.OS;
using Android.Views;
using Avalonia.Android.Platform;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace Avalonia.Android;

public class AvaloniaMainActivity<TApp> : AvaloniaMainActivity
    where TApp : Application, new()
{
    protected override AppBuilder CreateAppBuilder() => AppBuilder.Configure<TApp>().UseAndroid();
}
