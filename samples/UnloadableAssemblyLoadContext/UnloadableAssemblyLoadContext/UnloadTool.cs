using System;

namespace UnloadableAssemblyLoadContext;

public class UnloadTool
{
    public AssemblyLoadContextH AssemblyLoadContextH;
    public WeakReference Unload()
    {
       var weakReference = new WeakReference(AssemblyLoadContextH);
        AssemblyLoadContextH.Unload();
        AssemblyLoadContextH = null;
        return weakReference;
    }
}
