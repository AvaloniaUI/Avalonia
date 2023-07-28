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
            if (inner is IRootProvider root)
                root.FocusChanged += (_, _) => Node?.FocusChanged(); 
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
        public IAvnAutomationPeer? VisualRoot => Wrap(_inner.GetVisualRoot());

        public int HasKeyboardFocus() => _inner.HasKeyboardFocus().AsComBool();
        public int IsContentElement() => _inner.IsContentElement().AsComBool();
        public int IsControlElement() => _inner.IsControlElement().AsComBool();
        public int IsEnabled() => _inner.IsEnabled().AsComBool();
        public int IsKeyboardFocusable() => _inner.IsKeyboardFocusable().AsComBool();
        public void SetFocus() => _inner.SetFocus();
        public int ShowContextMenu() => _inner.ShowContextMenu().AsComBool();

        public void SetNode(IAvnAutomationNode node)
        {
            if (Node is not null)
                throw new InvalidOperationException("The AvnAutomationPeer already has a node.");
            Node = node;
        }

        public IAvnAutomationPeer? RootPeer
        {
            get
            {
                var peer = _inner;
                var parent = peer.GetParent();

                while (peer.GetProvider<IRootProvider>() is null && parent is not null)
                {
                    peer = parent;
                    parent = peer.GetParent();
                }

                return Wrap(peer);
            }
        }

        private IEmbeddedRootProvider EmbeddedRootProvider => GetProvider<IEmbeddedRootProvider>();
        private IExpandCollapseProvider ExpandCollapseProvider => GetProvider<IExpandCollapseProvider>();
        private IInvokeProvider InvokeProvider => GetProvider<IInvokeProvider>();
        private IRangeValueProvider RangeValueProvider => GetProvider<IRangeValueProvider>();
        private IRootProvider RootProvider => GetProvider<IRootProvider>();
        private ISelectionItemProvider SelectionItemProvider => GetProvider<ISelectionItemProvider>();
        private IToggleProvider ToggleProvider => GetProvider<IToggleProvider>();
        private IValueProvider ValueProvider => GetProvider<IValueProvider>();

        public int IsRootProvider() => IsProvider<IRootProvider>();

        public IAvnWindowBase? RootProvider_GetWindow() => (RootProvider.PlatformImpl as WindowBaseImpl)?.Native;
        public IAvnAutomationPeer? RootProvider_GetFocus() => Wrap(RootProvider.GetFocus());

        public IAvnAutomationPeer? RootProvider_GetPeerFromPoint(AvnPoint point)
        {
            var result = RootProvider.GetPeerFromPoint(point.ToAvaloniaPoint());

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


        public int IsEmbeddedRootProvider() => IsProvider<IEmbeddedRootProvider>();

        public IAvnAutomationPeer? EmbeddedRootProvider_GetFocus() => Wrap(EmbeddedRootProvider.GetFocus());

        public IAvnAutomationPeer? EmbeddedRootProvider_GetPeerFromPoint(AvnPoint point)
        {
            var result = EmbeddedRootProvider.GetPeerFromPoint(point.ToAvaloniaPoint());

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

        public int IsExpandCollapseProvider() => IsProvider<IExpandCollapseProvider>();

        public int ExpandCollapseProvider_GetIsExpanded() => ExpandCollapseProvider.ExpandCollapseState switch
        {
            ExpandCollapseState.Expanded => 1,
            ExpandCollapseState.PartiallyExpanded => 1,
            _ => 0,
        };

        public int ExpandCollapseProvider_GetShowsMenu() => ExpandCollapseProvider.ShowsMenu.AsComBool();
        public void ExpandCollapseProvider_Expand() => ExpandCollapseProvider.Expand();
        public void ExpandCollapseProvider_Collapse() => ExpandCollapseProvider.Collapse();

        public int IsInvokeProvider() => IsProvider<IInvokeProvider>();
        public void InvokeProvider_Invoke() => InvokeProvider.Invoke();

        public int IsRangeValueProvider() => IsProvider<IRangeValueProvider>();
        public double RangeValueProvider_GetValue() => RangeValueProvider.Value;
        public double RangeValueProvider_GetMinimum() => RangeValueProvider.Minimum;
        public double RangeValueProvider_GetMaximum() => RangeValueProvider.Maximum;
        public double RangeValueProvider_GetSmallChange() => RangeValueProvider.SmallChange;
        public double RangeValueProvider_GetLargeChange() => RangeValueProvider.LargeChange;
        public void RangeValueProvider_SetValue(double value) => RangeValueProvider.SetValue(value);

        public int IsSelectionItemProvider() => IsProvider<ISelectionItemProvider>();
        public int SelectionItemProvider_IsSelected() => SelectionItemProvider.IsSelected.AsComBool();
        
        public int IsToggleProvider() => IsProvider<IToggleProvider>();
        public int ToggleProvider_GetToggleState() => (int)ToggleProvider.ToggleState;
        public void ToggleProvider_Toggle() => ToggleProvider.Toggle();

        public int IsValueProvider() => IsProvider<IValueProvider>();
        public IAvnString ValueProvider_GetValue() => ValueProvider.Value.ToAvnString();
        public void ValueProvider_SetValue(string value) => ValueProvider.SetValue(value);

        [return: NotNullIfNotNull("peer")]
        public static AvnAutomationPeer? Wrap(AutomationPeer? peer)
        {
            return peer is null ? null : s_wrappers.GetValue(peer, x => new(peer));
        }

        private T GetProvider<T>()
        {
            return _inner.GetProvider<T>() ?? throw new InvalidOperationException(
                $"The peer {_inner} does not implement {typeof(T)}.");
        }

        private int IsProvider<T>() => (_inner.GetProvider<T>() is not null).AsComBool();
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
