using System;
using System.Collections.Generic;
using Avalonia.Controls.Primitives;

#nullable enable

namespace Avalonia.Controls.Automation.Peers
{
    public abstract class SelectingItemsControlAutomationPeer : ControlAutomationPeer,
        ISelectingAutomationPeer
    {
        protected SelectingItemsControlAutomationPeer(Control owner) : base(owner) { }

        public SelectionMode GetSelectionMode() => GetSelectionModeCore();
        public IReadOnlyList<AutomationPeer> GetSelection() => GetSelectionCore() ?? Array.Empty<AutomationPeer>();

        protected virtual IReadOnlyList<AutomationPeer>? GetSelectionCore()
        {
            List<AutomationPeer>? result = null;

            if (Owner is SelectingItemsControl owner)
            {
                var selection = Owner.GetValue(ListBox.SelectionProperty);

                foreach (var i in selection.SelectedIndexes)
                {
                    var container = owner.ItemContainerGenerator.ContainerFromIndex(i);

                    if (container is Control c)
                    {
                        var peer = GetOrCreatePeer(c);

                        if (peer is object)
                        {
                            result ??= new List<AutomationPeer>();
                            result.Add(peer);
                        }
                    }
                }

                return result;
            }

            return result;
        }

        protected virtual SelectionMode GetSelectionModeCore()
        {
            return (Owner as SelectingItemsControl)?.GetValue(ListBox.SelectionModeProperty) ?? SelectionMode.Single;
        }
    }
}
