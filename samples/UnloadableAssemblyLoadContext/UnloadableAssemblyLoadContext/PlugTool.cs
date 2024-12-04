using System;
using System.Linq;
using Avalonia.Controls;

namespace UnloadableAssemblyLoadContext;

public class PlugTool
{
    public AssemblyLoadContextH? AssemblyLoadContextH;
    public WeakReference Unload()
    {
       var weakReference = new WeakReference(AssemblyLoadContextH);
        AssemblyLoadContextH?.Unload();
        AssemblyLoadContextH = null;
        return weakReference;
    }

    public Control? FindControl(string type)
    {
        var type1 = AssemblyLoadContextH!.Assemblies.
                                         FirstOrDefault(x => x.GetName().Name == "UnloadableAssemblyLoadContextPlug")?.
                                         GetType(type);
        if (type1 is not null && type1.IsSubclassOf(typeof(Control)))
        {
            var constructorInfo = type1.GetConstructor([])!.Invoke(null) as Control;
            return constructorInfo;
        }

        return null;
    }
}
