using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Native.Interop;

#nullable enable

namespace Avalonia.Native
{
    internal class AvnAutomationPeer : NativeCallbackBase, IAvnAutomationPeer
    {
        private static readonly ConditionalWeakTable<AutomationPeer, AvnAutomationPeer> s_wrappers = new();
        private readonly AutomationPeer _inner;

        private AvnAutomationPeer(AutomationPeer inner)
        {
            _inner = inner;
            _inner.ChildrenChanged += (_, _) => Node?.ChildrenChanged();
            if (inner is WindowBaseAutomationPeer window)
                window.FocusChanged += (_, _) => Node?.FocusChanged(); 
        }

        ~AvnAutomationPeer() => Node?.Dispose();
        
        public IAvnAutomationNode? Node { get; private set; }
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

                while (peer is not IRootProvider && parent is not null)
                {
                    peer = parent;
                    parent = peer.GetParent();
                }

                return Wrap(peer);
            }
        }

        public void SetNode(IAvnAutomationNode node)
        {
            if (Node is not null)
                throw new InvalidOperationException("The AvnAutomationPeer already has a node.");
            Node = node;
        }
        
        public int IsRootProvider() => (_inner is IRootProvider).AsComBool();

        public IAvnWindowBase RootProvider_GetWindow()
        {
            var window = (WindowBase)((ControlAutomationPeer)_inner).Owner;
            return ((WindowBaseImpl)window.PlatformImpl!).Native;
        }
        
        public IAvnAutomationPeer? RootProvider_GetFocus() => Wrap(((IRootProvider)_inner).GetFocus());

        public IAvnAutomationPeer? RootProvider_GetPeerFromPoint(AvnPoint point)
        {
            var result = ((IRootProvider)_inner).GetPeerFromPoint(point.ToAvaloniaPoint());

            if (result is null)
                return null;

            // The OSX accessibility APIs expect non-ignored elements when hit-testing.
            while (!result.IsControlElement())
            {
                var parent = result.GetParent();

                if (parent is not null)
                    result = parent;
                else
                    break;
            }
            
            return Wrap(result);
        }

        public int IsExpandCollapseProvider() => (_inner is IExpandCollapseProvider).AsComBool();

        public int ExpandCollapseProvider_GetIsExpanded() => ((IExpandCollapseProvider)_inner).ExpandCollapseState switch
        {
            ExpandCollapseState.Expanded => 1,
            ExpandCollapseState.PartiallyExpanded => 1,
            _ => 0,
        };

        public int ExpandCollapseProvider_GetShowsMenu() => ((IExpandCollapseProvider)_inner).ShowsMenu.AsComBool();
        public void ExpandCollapseProvider_Expand() => ((IExpandCollapseProvider)_inner).Expand();
        public void ExpandCollapseProvider_Collapse() => ((IExpandCollapseProvider)_inner).Collapse();

        public int IsInvokeProvider() => (_inner is IInvokeProvider).AsComBool();
        public void InvokeProvider_Invoke() => ((IInvokeProvider)_inner).Invoke();

        public int IsRangeValueProvider() => (_inner is IRangeValueProvider).AsComBool();
        public double RangeValueProvider_GetValue() => ((IRangeValueProvider)_inner).Value;
        public double RangeValueProvider_GetMinimum() => ((IRangeValueProvider)_inner).Minimum;
        public double RangeValueProvider_GetMaximum() => ((IRangeValueProvider)_inner).Maximum;
        public double RangeValueProvider_GetSmallChange() => ((IRangeValueProvider)_inner).SmallChange;
        public double RangeValueProvider_GetLargeChange() => ((IRangeValueProvider)_inner).LargeChange;
        public void RangeValueProvider_SetValue(double value) => ((IRangeValueProvider)_inner).SetValue(value);

        public int IsSelectionItemProvider() => (_inner is ISelectionItemProvider).AsComBool();
        public int SelectionItemProvider_IsSelected() => ((ISelectionItemProvider)_inner).IsSelected.AsComBool();
        
        public int IsToggleProvider() => (_inner is IToggleProvider).AsComBool();
        public int ToggleProvider_GetToggleState() => (int)((IToggleProvider)_inner).ToggleState;
        public void ToggleProvider_Toggle() => ((IToggleProvider)_inner).Toggle();

        public int IsValueProvider() => (_inner is IValueProvider).AsComBool();
        public IAvnString ValueProvider_GetValue() => ((IValueProvider)_inner).Value.ToAvnString();
        public void ValueProvider_SetValue(string value) => ((IValueProvider)_inner).SetValue(value);

        [return: NotNullIfNotNull("peer")]
        public static AvnAutomationPeer? Wrap(AutomationPeer? peer)
        {
            return peer is null ? null : s_wrappers.GetValue(peer, x => new(peer));
        }
    }

    internal class AvnAutomationPeerArray : NativeCallbackBase, IAvnAutomationPeerArray
    {
        private readonly AvnAutomationPeer[] _items;
        
        public AvnAutomationPeerArray(IReadOnlyList<AutomationPeer> items)
        {
            _items = items.Select(x => AvnAutomationPeer.Wrap(x)).ToArray();
        }
        
        public uint Count => (uint)_items.Length;
        public IAvnAutomationPeer Get(uint index) => _items[index];
    }
}
