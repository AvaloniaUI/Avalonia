#region

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;

#endregion

namespace UnloadableAssemblyLoadContext;

public class AssemblyLoadContextH : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public AssemblyLoadContextH(string pluginPath, string name) : base(isCollectible: true, name: name)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        Unloading += (sender) =>
        {
            AvaloniaPropertyRegistry.Instance.UnregisterByModule(sender.Assemblies.First().DefinedTypes);

            if (MainWindow.Style is { } style)
                Application.Current?.Styles.Remove(style);

            AssetLoader.InvalidateAssemblyCache(sender.Assemblies.First().GetName().Name!);
            MainWindow.Style = null;
        };
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            if (assemblyPath.EndsWith("WinRT.Runtime.dll") || assemblyPath.EndsWith("Microsoft.Windows.SDK.NET.dll")|| assemblyPath.EndsWith("Avalonia.Controls.dll")|| assemblyPath.EndsWith("Avalonia.Base.dll")|| assemblyPath.EndsWith("Avalonia.Markup.Xaml.dll"))
            {
                return null;
            }

            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
