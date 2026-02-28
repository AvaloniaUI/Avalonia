using Avalonia.Input;

namespace Avalonia.Controls;

/// <summary>
/// For now this is a stub class that is needed to prevent people from assuming that TopLevel sits at the root of the
/// visual tree.
/// In future 12.x releases it will serve more roles like hosting popups and CSD.
/// </summary>
internal class TopLevelHost : Control
{
    static TopLevelHost()
    {
        KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<TopLevelHost>(KeyboardNavigationMode.Cycle);
    }

    public TopLevelHost(TopLevel tl)
    {
        VisualChildren.Add(tl);
    }

    protected override bool BypassFlowDirectionPolicies => true;
}
