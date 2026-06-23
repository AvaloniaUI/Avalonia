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
            return ComputeStatesCore();
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
                    case ToggleState.Off:
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

            // Selection item states
            if (Peer.GetProvider<ISelectionItemProvider>() is { } selectionItem)
            {
                states.Add(AtSpiState.Selectable);
                if (selectionItem.IsSelected)
                    states.Add(AtSpiState.Selected);
            }

            // Multi-selectable container
            if (Peer.GetProvider<ISelectionProvider>() is { CanSelectMultiple: true })
                states.Add(AtSpiState.MultiSelectable);

            // Value provider states (text editable/read-only)
            if (Peer.GetProvider<IValueProvider>() is { } valueProvider)
            {
                if (valueProvider.IsReadOnly)
                    states.Add(AtSpiState.ReadOnly);
                else
                    states.Add(AtSpiState.Editable);
            }

            // Range value read-only
            if (Peer.GetProvider<IRangeValueProvider>() is { IsReadOnly: true })
                states.Add(AtSpiState.ReadOnly);

            // Required for form
            if (Peer is ControlAutomationPeer controlPeer &&
                AutomationProperties.GetIsRequiredForForm(controlPeer.Owner))
                states.Add(AtSpiState.Required);

            // Window-level active state and text entry states
            var controlType = Peer.GetAutomationControlType();
            if (controlType == AutomationControlType.Window)
                states.Add(AtSpiState.Active);

            if (controlType == AutomationControlType.Edit)
                states.Add(AtSpiState.SingleLine);

            return BuildStateSet(states);
        }
    }
}
