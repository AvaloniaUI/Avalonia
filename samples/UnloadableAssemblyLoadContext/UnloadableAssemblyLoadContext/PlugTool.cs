using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;

namespace UnloadableAssemblyLoadContext;

public class PlugTool
{
    private AssemblyLoadContextH? _context;
    private StyleInclude? _style;
    private Window? _window;

    public PlugTool(string pluginPath)
    {
        _context = new AssemblyLoadContextH(pluginPath, "sample-plugin");
        try
        {
            var assembly = _context.LoadFromAssemblyPath(pluginPath);
            _style = new StyleInclude(new Uri("avares://UnloadableAssemblyLoadContextPlug/"))
            {
                Source = new Uri("ControlStyle.axaml", UriKind.Relative)
            };
            Application.Current!.Styles.Add(_style);
            _window = (Window)Activator.CreateInstance(
                assembly.GetType("UnloadableAssemblyLoadContextPlug.Window1", throwOnError: true)!)!;
            _window.Show();
        }
        catch
        {
            Unload();
            throw;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public WeakReference Unload()
    {
        _window?.Close();
        _window = null;
        if (_style is not null)
            Application.Current?.Styles.Remove(_style);
        _style = null;

        var context = _context;
        _context = null;
        var reference = new WeakReference(context, trackResurrection: true);
        context?.Unload();
        return reference;
    }
}
