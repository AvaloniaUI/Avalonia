using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;

namespace Avalonia.Controls;

partial class TopLevelHost
{
    
    protected override AutomationPeer OnCreateAutomationPeer()
        => new TopLevelHostAutomationPeer(this);

    private DecorationsOverlaysAutomationPeer? _decorationsOverlayPeer;

    public AutomationPeer GetOrCreateDecorationsOverlaysPeer() =>
        _decorationsOverlayPeer ??= new DecorationsOverlaysAutomationPeer(this, _topLevel);
    
    /// <summary>
    /// Automation peer that returns no children. The automation tree is managed
    /// by WindowAutomationPeer, which directly includes decoration content.
    /// Without this, EnsureConnected would walk up through TopLevelHost and
    /// set Window's parent peer to TopLevelHost's peer, breaking the root.
    /// </summary>
    private class TopLevelHostAutomationPeer(TopLevelHost owner) : ControlAutomationPeer(owner)
    {
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;
    }
    
    private class DecorationsOverlaysAutomationPeer(TopLevelHost host, TopLevel topLevel) : AutomationPeer
    {
        private List<AutomationPeer> _children = new();
        private bool _childrenValid = false;
        protected override void BringIntoViewCore() => topLevel.GetOrCreateAutomationPeer().BringIntoView();

        protected override string? GetAcceleratorKeyCore() => null;

        protected override string? GetAccessKeyCore() => null;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        
        protected override string? GetAutomationIdCore() => "AvaloniaWindowChrome";

        protected override Rect GetBoundingRectangleCore() => host.Bounds;

        protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore()
        {
            if (!_childrenValid)
            {
                var newChildren = (new LayerWrapper?[] { host._fullscreenPopover, host._overlay, host._underlay })
                    .Where(c => c?.IsVisible == true)
                    .Select(c => c!.GetOrCreateAutomationPeer()).ToList();
                
                foreach (var peer in _children.Except(newChildren))
                    peer.TrySetParent(null);
                foreach (var peer in newChildren)
                    peer.TrySetParent(this);
                _children = newChildren;
            }

            return _children;

        }

        public void InvalidateChildren()
        {
            if (_childrenValid)
            {
                _childrenValid = false;
                RaiseChildrenChangedEvent();
            }
        }

        protected override string GetClassNameCore() => "WindowChrome";

        protected override AutomationPeer? GetLabeledByCore() => null;

        protected override string? GetNameCore() => "WindowChrome";

        protected override AutomationPeer? GetParentCore() => topLevel.GetOrCreateAutomationPeer();
        
        protected override bool HasKeyboardFocusCore() => _children?.Any(x => x.HasKeyboardFocus()) == true;
        protected override bool IsKeyboardFocusableCore() => false;

        protected override bool IsContentElementCore() => false;

        protected override bool IsControlElementCore() => true;

        protected override bool IsEnabledCore() => true;

        protected override void SetFocusCore(){}

        protected override bool ShowContextMenuCore() => false;

        protected internal override bool TrySetParent(AutomationPeer? parent) => false;
    }
}
