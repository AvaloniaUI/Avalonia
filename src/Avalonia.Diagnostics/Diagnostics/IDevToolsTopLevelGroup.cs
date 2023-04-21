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
    public ClassicDesktopStyleApplicationLifetimeTopLevelGroup(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        Items = lifetime?.Windows ?? throw new ArgumentNullException(nameof(lifetime));
    }
    
    public IReadOnlyList<TopLevel> Items { get; }
}

internal class SingleViewTopLevelGroup : IDevToolsTopLevelGroup
{
    public SingleViewTopLevelGroup(TopLevel topLevel)
    {
        Items = new[] { topLevel ?? throw new ArgumentNullException(nameof(topLevel)) };
    }
    
    public IReadOnlyList<TopLevel> Items { get; }
}
