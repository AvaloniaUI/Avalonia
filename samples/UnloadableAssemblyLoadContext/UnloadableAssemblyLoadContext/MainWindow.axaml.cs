using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace UnloadableAssemblyLoadContext;

public partial class MainWindow : Window
{
    private PlugTool? _plugin;
    private WeakReference? _pendingUnload;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        LoadPlugin();
    }

    private void OnLoadClick(object? sender, RoutedEventArgs e) => LoadPlugin();

    private void LoadPlugin()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Plugin", "UnloadableAssemblyLoadContextPlug.dll");
            _plugin = new PlugTool(path);
            this.FindControl<TextBlock>("Status")!.Text = "Plugin loaded. Unload it, then load it again.";
            this.FindControl<Button>("LoadButton")!.IsEnabled = false;
            this.FindControl<Button>("UnloadButton")!.IsEnabled = true;
        }
        catch (Exception exception)
        {
            this.FindControl<TextBlock>("Status")!.Text = exception.Message;
        }
    }

    private async void OnUnloadClick(object? sender, RoutedEventArgs e)
    {
        var loadButton = this.FindControl<Button>("LoadButton")!;
        var unloadButton = this.FindControl<Button>("UnloadButton")!;
        unloadButton.IsEnabled = false;
        if (_plugin is not null)
        {
            _pendingUnload = _plugin.Unload();
            _plugin = null;
        }

        // Let plugin frames and UI cleanup unwind before checking collection.
        for (var attempt = 0; attempt < 20 && _pendingUnload?.IsAlive == true; ++attempt)
        {
            await Task.Delay(50);
            GC.Collect();
        }

        var retained = _pendingUnload?.IsAlive == true;
        this.FindControl<TextBlock>("Status")!.Text = retained
            ? "Plugin is still referenced. Release its resources and retry unloading."
            : "Plugin context collected. Load it again to repeat the cycle.";
        loadButton.IsEnabled = !retained;
        unloadButton.IsEnabled = retained;
        if (!retained)
            _pendingUnload = null;
    }

    protected override void OnClosed(EventArgs e)
    {
        _plugin?.Unload();
        _plugin = null;
        base.OnClosed(e);
    }
}
