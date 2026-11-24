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
            var match = loadedAssemblies.FirstOrDefault(a => IsScopeNameOf(a.ManifestModule.ScopeName, name));
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
    /// <summary>
    /// Whether <paramref name="scopeName"/>, the name of an assembly's manifest module, belongs to the assembly
    /// <paramref name="name"/>: it is the file name of the module ("Lib.dll"), or the assembly name itself when the
    /// module has no file. A longer name which merely starts with <paramref name="name"/> ("Lib.Tests.dll") doesn't.
    /// </summary>
    internal static bool IsScopeNameOf(string scopeName, string name)
    {
        if (!scopeName.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            return false;

        var extensionLength = scopeName.Length - name.Length;
        return extensionLength == 0
               || (extensionLength == 4
                   && (string.Compare(scopeName, name.Length, ".dll", 0, 4, StringComparison.OrdinalIgnoreCase) == 0
                       || string.Compare(scopeName, name.Length, ".exe", 0, 4, StringComparison.OrdinalIgnoreCase) == 0));
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
