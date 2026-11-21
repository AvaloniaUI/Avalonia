#region

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Avalonia;
using Avalonia.Platform;

#endregion

namespace UnloadableAssemblyLoadContext;

public class AssemblyLoadContextH : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public AssemblyLoadContextH(string pluginPath, string name) : base(isCollectible: true, name: name)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        Unloading += sender =>
        {
            var assemblies = sender.Assemblies.ToArray();
            if (!AvaloniaPropertyRegistry.Instance.UnregisterByModule(assemblies.SelectMany(assembly => assembly.DefinedTypes)))
                throw new InvalidOperationException("Plugin property cleanup failed.");
            foreach (var assembly in assemblies)
                AssetLoader.InvalidateAssemblyCache(assembly.GetName().Name!);
        };
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? string.Empty;
        if (name.StartsWith("Avalonia", StringComparison.Ordinal) ||
            name is "WinRT.Runtime" or "Microsoft.Windows.SDK.NET")
            return null;

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
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
