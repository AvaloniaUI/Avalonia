using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Diagnostics;

internal interface IDevToolsTopLevelGroup
{
    IReadOnlyList<TopLevel> Items { get; }
}

internal class ClassicDesktopStyleApplicationLifetimeTopLevelGroup : IDevToolsTopLevelGroup
{
    private readonly IClassicDesktopStyleApplicationLifetime _lifetime;

    public ClassicDesktopStyleApplicationLifetimeTopLevelGroup(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    }

    public IReadOnlyList<TopLevel> Items => _lifetime.Windows;

    public override int GetHashCode()
    {
        return _lifetime.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is ClassicDesktopStyleApplicationLifetimeTopLevelGroup g && g._lifetime == _lifetime;
    }
}

internal class SingleViewTopLevelGroup : IDevToolsTopLevelGroup
{
    private readonly TopLevel _topLevel;

    public SingleViewTopLevelGroup(TopLevel topLevel)
    {
        _topLevel = topLevel;
        Items = new[] { topLevel ?? throw new ArgumentNullException(nameof(topLevel)) };
    }
    
    public IReadOnlyList<TopLevel> Items { get; }
    
    public override int GetHashCode()
    {
        return _topLevel.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is SingleViewTopLevelGroup g && g._topLevel == _topLevel;
    }
}
