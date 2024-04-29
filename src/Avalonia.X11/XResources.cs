#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Avalonia.X11.XLib;
namespace Avalonia.X11;

internal class XResources
{
    private Dictionary<string, string> _resources = new();
    private readonly X11Info _x11;
    public event Action<string>? ResourceChanged;

    public XResources(AvaloniaX11Platform plat)
    {
        _x11 = plat.Info;
        plat.Globals.RootPropertyChanged += OnRootPropertyChanged;
        UpdateResources();
    }

    void UpdateResources()
    {
        var res = ReadResourcesString() ?? "";
        var items = res.Split('\n');
        var newResources = new Dictionary<string, string>();
        var missingResources = new HashSet<string>(_resources.Keys);
        var changedResources = new HashSet<string>();
        foreach (var item in items)
        {
            var sp = item.Split(new[] { ':' }, 2);
            if (sp.Length < 2)
                continue;
            var key = sp[0];
            var value = sp[1].TrimStart();
            newResources[key] = value;
            if (!missingResources.Remove(sp[0]) || _resources[key] != value)
                changedResources.Add(key);
        }
        _resources = newResources;
        foreach (var missing in missingResources)
            ResourceChanged?.Invoke(missing);
        foreach (var changed in changedResources)
            ResourceChanged?.Invoke(changed);
    }

    public string? GetResource(string key)
    {
        _resources.TryGetValue(key, out var value);
        return value;
    }
    
    string? ReadResourcesString()
    {
        XGetWindowProperty(_x11.Display, _x11.RootWindow, _x11.Atoms.XA_RESOURCE_MANAGER,
            IntPtr.Zero, new IntPtr(0x7fffffff),
            false, _x11.Atoms.XA_STRING, out _, out var actualFormat,
            out var nitems, out _, out var prop);
        try
        {
            if (actualFormat != 8)
                return null;
            return Marshal.PtrToStringAnsi(prop, nitems.ToInt32());
        }
        finally
        {
            XFree(prop);
        }
    }
    
    private void OnRootPropertyChanged(IntPtr atom)
    {
        if (atom == _x11.Atoms.XA_RESOURCE_MANAGER)
            UpdateResources();
    }
}
