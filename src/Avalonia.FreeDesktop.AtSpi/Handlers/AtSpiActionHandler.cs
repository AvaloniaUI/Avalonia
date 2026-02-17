using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Automation.Provider;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    /// <summary>
    /// Implements the AT-SPI Action interface (invoke, toggle, expand/collapse, scroll).
    /// </summary>
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
            if (index >= 0 && index < _actions.Count)
                return ValueTask.FromResult(_actions[index].KeyBinding);
            return ValueTask.FromResult(string.Empty);
        }

        public ValueTask<List<AtSpiAction>> GetActionsAsync()
        {
            var result = new List<AtSpiAction>(_actions.Count);
            result.AddRange(_actions.Select(entry => new AtSpiAction(entry.LocalizedName, entry.Description, entry.KeyBinding)));
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
                case "expand or collapse":
                    if (_node.Peer.GetProvider<IExpandCollapseProvider>() is { } expandCollapseAction)
                    {
                        if (expandCollapseAction.ExpandCollapseState == ExpandCollapseState.Collapsed)
                            expandCollapseAction.Expand();
                        else
                            expandCollapseAction.Collapse();
                    }
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
        // TODO: Proper mapping of ActionList keybindings to AutomationPeers. 
        private List<ActionEntry> BuildActionList()
        {
            var actions = new List<ActionEntry>();

            if (_node.Peer.GetProvider<IInvokeProvider>() is not null)
            {
                var acceleratorKey = _node.Peer.GetAcceleratorKey() ?? string.Empty;
                actions.Add(new ActionEntry("click", "Click", "Performs the default action", acceleratorKey));
            }

            if (_node.Peer.GetProvider<IToggleProvider>() is not null)
                actions.Add(new ActionEntry("toggle", "Toggle", "Toggles the control state", string.Empty));

            if (_node.Peer.GetProvider<IExpandCollapseProvider>() is not null)
                actions.Add(new ActionEntry("expand or collapse", "Expand or Collapse", "Expands or collapses the control", string.Empty));

            if (_node.Peer.GetProvider<IScrollProvider>() is { } scroll)
            {
                if (scroll.VerticallyScrollable)
                {
                    actions.Add(new ActionEntry("scroll up", "Scroll Up", "Scrolls the view up", string.Empty));
                    actions.Add(new ActionEntry("scroll down", "Scroll Down", "Scrolls the view down", string.Empty));
                }

                if (scroll.HorizontallyScrollable)
                {
                    actions.Add(new ActionEntry("scroll left", "Scroll Left", "Scrolls the view left", string.Empty));
                    actions.Add(new ActionEntry("scroll right", "Scroll Right", "Scrolls the view right", string.Empty));
                }
            }
            
            if (_node.Peer.GetProvider<ISelectionItemProvider>() is not null
                && _node.Peer.GetProvider<IInvokeProvider>() is null)
            {
                actions.Add(new ActionEntry("select", "Select", "Selects this item", string.Empty));
            }

            return actions;
        }

        /// <summary>
        /// Describes a single AT-SPI action exposed by a node.
        /// </summary>
        private readonly record struct ActionEntry(
            string ActionName,
            string LocalizedName,
            string Description,
            string KeyBinding);
    }
}
