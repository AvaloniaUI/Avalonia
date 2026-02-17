using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    internal partial class AtSpiNode
    {
        public List<uint> ComputeStates()
        {
            return InvokeSync(() => ComputeStatesCore());
        }

        private List<uint> ComputeStatesCore()
        {
            var states = new HashSet<AtSpiState>();

            if (Peer.IsEnabled())
            {
                states.Add(AtSpiState.Enabled);
                states.Add(AtSpiState.Sensitive);
            }

            if (!Peer.IsOffscreen())
            {
                states.Add(AtSpiState.Visible);
                states.Add(AtSpiState.Showing);
            }

            if (Peer.IsKeyboardFocusable())
                states.Add(AtSpiState.Focusable);

            if (Peer.HasKeyboardFocus())
                states.Add(AtSpiState.Focused);

            // Toggle state
            if (Peer.GetProvider<IToggleProvider>() is { } toggle)
            {
                states.Add(AtSpiState.Checkable);
                switch (toggle.ToggleState)
                {
                    case ToggleState.On:
                        states.Add(AtSpiState.Checked);
                        break;
                    case ToggleState.Indeterminate:
                        states.Add(AtSpiState.Indeterminate);
                        break;
                }
            }

            // Expand/collapse state
            if (Peer.GetProvider<IExpandCollapseProvider>() is { } expandCollapse)
            {
                states.Add(AtSpiState.Expandable);
                switch (expandCollapse.ExpandCollapseState)
                {
                    case ExpandCollapseState.Expanded:
                        states.Add(AtSpiState.Expanded);
                        break;
                    case ExpandCollapseState.Collapsed:
                        states.Add(AtSpiState.Collapsed);
                        break;
                }
            }

            // Range value read-only
            if (Peer.GetProvider<IRangeValueProvider>() is { } rangeValue)
            {
                if (rangeValue.IsReadOnly)
                    states.Add(AtSpiState.ReadOnly);
            }

            // Window-level active state
            var controlType = Peer.GetAutomationControlType();
            if (controlType == AutomationControlType.Window)
                states.Add(AtSpiState.Active);

            return BuildStateSet(states);
        }
    }
}
