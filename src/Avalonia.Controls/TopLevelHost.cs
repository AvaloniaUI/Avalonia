using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Controls;

/// <summary>
/// Hosts the TopLevel and, when enabled, drawn decoration layers (underlay, overlay, fullscreen popover).
/// Serves as the visual root for PresentationSource.
/// </summary>
internal partial class TopLevelHost : Control
{
    static TopLevelHost()
    {
        KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<TopLevelHost>(KeyboardNavigationMode.Cycle);
    }

    public TopLevelHost(TopLevel tl)
    {
        _topLevel = tl;
        VisualChildren.Add(tl);
    }
}
