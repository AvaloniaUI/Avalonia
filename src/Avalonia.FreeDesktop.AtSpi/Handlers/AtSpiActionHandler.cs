using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Automation.Provider;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    internal sealed class AtSpiActionHandler : IOrgA11yAtspiAction
    {
        private readonly AtSpiNode _node;
        private readonly List<ActionEntry> _actions;

        public AtSpiActionHandler(AtSpiServer server, AtSpiNode node)
        {
            _ = server;
            _node = node;
            _actions = BuildActionList();
        }

        public uint Version => ActionVersion;

        public int NActions => _actions.Count;

        public ValueTask<string> GetDescriptionAsync(int index)
        {
            if (index >= 0 && index < _actions.Count)
                return ValueTask.FromResult(_actions[index].Description);
            return ValueTask.FromResult(string.Empty);
        }

        public ValueTask<string> GetNameAsync(int index)
        {
            if (index >= 0 && index < _actions.Count)
                return ValueTask.FromResult(_actions[index].ActionName);
            return ValueTask.FromResult(string.Empty);
        }

        public ValueTask<string> GetLocalizedNameAsync(int index)
        {
            if (index >= 0 && index < _actions.Count)
                return ValueTask.FromResult(_actions[index].LocalizedName);
            return ValueTask.FromResult(string.Empty);
        }

        public ValueTask<string> GetKeyBindingAsync(int index)
        {
            return ValueTask.FromResult(string.Empty);
        }

        public ValueTask<List<AtSpiAction>> GetActionsAsync()
        {
            var result = new List<AtSpiAction>(_actions.Count);
            foreach (var entry in _actions)
                result.Add(new AtSpiAction(entry.LocalizedName, entry.Description, string.Empty));
            return ValueTask.FromResult(result);
        }

        public ValueTask<bool> DoActionAsync(int index)
        {
            if (index < 0 || index >= _actions.Count)
                return ValueTask.FromResult(false);

            var action = _actions[index];
            ExecuteAction(action.ActionName);
            return ValueTask.FromResult(true);
        }

        private void ExecuteAction(string actionName)
        {
            switch (actionName)
            {
                case "click":
                    _node.Peer.GetProvider<IInvokeProvider>()?.Invoke();
                    break;
                case "toggle":
                    _node.Peer.GetProvider<IToggleProvider>()?.Toggle();
                    break;
                case "expand":
                    _node.Peer.GetProvider<IExpandCollapseProvider>()?.Expand();
                    break;
                case "collapse":
                    _node.Peer.GetProvider<IExpandCollapseProvider>()?.Collapse();
                    break;
                case "scroll up":
                    _node.Peer.GetProvider<IScrollProvider>()?.Scroll(
                        ScrollAmount.NoAmount, ScrollAmount.SmallDecrement);
                    break;
                case "scroll down":
                    _node.Peer.GetProvider<IScrollProvider>()?.Scroll(
                        ScrollAmount.NoAmount, ScrollAmount.SmallIncrement);
                    break;
                case "scroll left":
                    _node.Peer.GetProvider<IScrollProvider>()?.Scroll(
                        ScrollAmount.SmallDecrement, ScrollAmount.NoAmount);
                    break;
                case "scroll right":
                    _node.Peer.GetProvider<IScrollProvider>()?.Scroll(
                        ScrollAmount.SmallIncrement, ScrollAmount.NoAmount);
                    break;
                // Provisional: activate selectable items (TabItem, ListBoxItem) via Action.
                case "select":
                    _node.Peer.GetProvider<ISelectionItemProvider>()?.Select();
                    break;
            }
        }

        private List<ActionEntry> BuildActionList()
        {
            var actions = new List<ActionEntry>();

            if (_node.Peer.GetProvider<IInvokeProvider>() is not null)
                actions.Add(new ActionEntry("click", "Click", "Performs the default action"));

            if (_node.Peer.GetProvider<IToggleProvider>() is not null)
                actions.Add(new ActionEntry("toggle", "Toggle", "Toggles the control state"));

            if (_node.Peer.GetProvider<IExpandCollapseProvider>() is { } expandCollapse)
            {
                if (expandCollapse.ExpandCollapseState == ExpandCollapseState.Collapsed)
                    actions.Add(new ActionEntry("expand", "Expand", "Expands the control"));
                else
                    actions.Add(new ActionEntry("collapse", "Collapse", "Collapses the control"));
            }

            if (_node.Peer.GetProvider<IScrollProvider>() is { } scroll)
            {
                if (scroll.VerticallyScrollable)
                {
                    actions.Add(new ActionEntry("scroll up", "Scroll Up", "Scrolls the view up"));
                    actions.Add(new ActionEntry("scroll down", "Scroll Down", "Scrolls the view down"));
                }

                if (scroll.HorizontallyScrollable)
                {
                    actions.Add(new ActionEntry("scroll left", "Scroll Left", "Scrolls the view left"));
                    actions.Add(new ActionEntry("scroll right", "Scroll Right", "Scrolls the view right"));
                }
            }

            // Provisional: expose a "select" action for items that implement ISelectionItemProvider
            // (e.g. TabItem, ListBoxItem) but lack IInvokeProvider. This allows screen readers to
            // activate selectable items. Remove once core automation peers provide IInvokeProvider
            // or the parent container properly exposes ISelectionProvider.
            if (_node.Peer.GetProvider<ISelectionItemProvider>() is not null
                && _node.Peer.GetProvider<IInvokeProvider>() is null)
            {
                actions.Add(new ActionEntry("select", "Select", "Selects this item"));
            }

            return actions;
        }

        private readonly record struct ActionEntry(string ActionName, string LocalizedName, string Description);
    }
}
