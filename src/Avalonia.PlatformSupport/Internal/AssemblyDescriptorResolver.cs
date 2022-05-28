using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Avalonia.PlatformSupport.Internal;

internal interface IAssemblyDescriptorResolver
{
    IAssemblyDescriptor GetAssembly(string name);
}

internal class AssemblyDescriptorResolver: IAssemblyDescriptorResolver
{
    private readonly Dictionary<string, IAssemblyDescriptor> _assemblyNameCache = new();

    public IAssemblyDescriptor GetAssembly(string name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        if (!_assemblyNameCache.TryGetValue(name, out var rv))
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var match = loadedAssemblies.FirstOrDefault(a => a.GetName().Name == name);
            if (match != null)
            {
                _assemblyNameCache[name] = rv = new AssemblyDescriptor(match);
            }
            else
            {
                // iOS does not support loading assemblies dynamically!
#if NET6_0_OR_GREATER
                if (OperatingSystem.IsIOS())
                {
                    throw new InvalidOperationException(
                        $"Assembly {name} needs to be referenced and explicitly loaded before loading resources");
                }
#endif
                name = Uri.UnescapeDataString(name);
                _assemblyNameCache[name] = rv = new AssemblyDescriptor(Assembly.Load(name));
            }
        }

        return rv;
    }
}
