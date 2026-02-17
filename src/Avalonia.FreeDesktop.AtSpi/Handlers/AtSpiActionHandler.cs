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
            _node.InvokeSync(() => ExecuteAction(action.ActionName));
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

            return actions;
        }

        private readonly record struct ActionEntry(string ActionName, string LocalizedName, string Description);
    }
}
