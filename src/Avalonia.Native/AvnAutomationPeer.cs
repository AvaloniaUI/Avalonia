using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Native.Interop;

#nullable enable

namespace Avalonia.Native
{
    internal class AvnAutomationPeer : CallbackBase, IAvnAutomationPeer
    {
        private readonly AutomationPeer _inner;

        public AvnAutomationPeer(AutomationPeer inner) => _inner = inner;

        public IAvnAutomationNode Node => ((AutomationNode)_inner.Node).Native;
        public IAvnString? AcceleratorKey => _inner.GetAcceleratorKey().ToAvnString();
        public IAvnString? AccessKey => _inner.GetAccessKey().ToAvnString();
        public AvnAutomationControlType AutomationControlType => (AvnAutomationControlType)_inner.GetAutomationControlType();
        public IAvnString? AutomationId => _inner.GetAutomationId().ToAvnString();
        public AvnRect BoundingRectangle => _inner.GetBoundingRectangle().ToAvnRect();
        public IAvnAutomationPeerArray Children => new AvnAutomationPeerArray(_inner.GetChildren());
        public IAvnString ClassName => _inner.GetClassName().ToAvnString();
        public IAvnAutomationPeer? LabeledBy => Wrap(_inner.GetLabeledBy());
        public IAvnString Name => _inner.GetName().ToAvnString();
        public IAvnAutomationPeer? Parent => Wrap(_inner.GetParent());

        public int HasKeyboardFocus() => _inner.HasKeyboardFocus().AsComBool();
        public int IsContentElement() => _inner.IsContentElement().AsComBool();
        public int IsControlElement() => _inner.IsControlElement().AsComBool();
        public int IsEnabled() => _inner.IsEnabled().AsComBool();
        public int IsKeyboardFocusable() => _inner.IsKeyboardFocusable().AsComBool();
        public void SetFocus() => _inner.SetFocus();
        public int ShowContextMenu() => _inner.ShowContextMenu().AsComBool();

        public IAvnAutomationPeer? RootPeer
        {
            get
            {
                var peer = _inner;
                var parent = peer.GetParent();

                while (!(peer is IRootProvider) && parent is object)
                {
                    peer = parent;
                    parent = peer.GetParent();
                }

                return new AvnAutomationPeer(peer);
            }
        }

        public int IsRootProvider() => (_inner is IRootProvider).AsComBool();

        public IAvnAutomationPeer? RootProvider_GetPeerFromPoint(AvnPoint point)
        {
            var result = ((IRootProvider)_inner).GetPeerFromPoint(point.ToAvaloniaPoint());

            if (result is null)
                return null;

            // The OSX accessibility APIs expect non-ignored elements when hit-testing.
            while (!result.IsControlElement())
            {
                var parent = result.GetParent();

                if (parent is object)
                    result = parent;
                else
                    break;
            }
            
            return Wrap(result);
        }

        public int IsInvokeProvider() => (_inner is IInvokeProvider).AsComBool();

        public void InvokeProvider_Invoke() => ((IInvokeProvider)_inner).Invoke();
        
        public static AvnAutomationPeer? Wrap(AutomationPeer? peer) =>
            peer != null ? new AvnAutomationPeer(peer) : null;
    }

    internal class AvnAutomationPeerArray : CallbackBase, IAvnAutomationPeerArray
    {
        private readonly AvnAutomationPeer[] _items;
        
        public AvnAutomationPeerArray(IReadOnlyList<AutomationPeer> items)
        {
            _items = items.Select(x => new AvnAutomationPeer(x)).ToArray();
        }
        
        public uint Count => (uint)_items.Length;
        public IAvnAutomationPeer Get(uint index) => _items[index];
    }
}
