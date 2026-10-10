using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Avalonia.Platform.Internal;

internal interface IAssemblyDescriptorResolver
{
    IAssemblyDescriptor GetAssembly(string name);
    void InvalidateAssemblyCache(string name);
    void InvalidateAssemblyCache();
    
}

internal class AssemblyDescriptorResolver: IAssemblyDescriptorResolver
{
    private readonly ConcurrentDictionary<string, IAssemblyDescriptor> _assemblyNameCache = new();

    public IAssemblyDescriptor GetAssembly(string name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        return _assemblyNameCache.GetOrAdd(name, static name =>
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            // Select assembly by ManifestModule.ScopeName instead of GetName().Name to avoid CultureInfo dependency.
            var match = loadedAssemblies.Where(a => a.ManifestModule.ScopeName.StartsWith(name, StringComparison.InvariantCultureIgnoreCase)).OrderBy(a => a.ManifestModule.ScopeName.Length).FirstOrDefault();
            if (match != null)
            {
                return new AssemblyDescriptor(match);
            }
            else
            {
#if NET6_0_OR_GREATER
                if (!RuntimeFeature.IsDynamicCodeSupported)
                {
                    throw new InvalidOperationException(
                        $"Assembly {name} needs to be referenced and explicitly loaded before loading resources");
                }
#endif
                name = Uri.UnescapeDataString(name);
                return new AssemblyDescriptor(Assembly.Load(name));
            }
        });
    }
    public void InvalidateAssemblyCache(string name)
    {
        _assemblyNameCache.TryRemove(name, out _);
    }

    public void InvalidateAssemblyCache()
    {
        _assemblyNameCache.Clear();
    }
}
