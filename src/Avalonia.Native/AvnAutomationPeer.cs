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
using Avalonia.Utilities;

#nullable enable

namespace Avalonia.Native
{
    internal class AvnAutomationPeer : NativeCallbackBase, IAvnAutomationPeer, IWeakEventSubscriber<EventArgs>
    {
        private static readonly WeakEvent<AutomationPeer, EventArgs> ChildrenChangedWeakEvent = WeakEvent.Register<AutomationPeer>(
                (s, h) => s.ChildrenChanged += h,
                (s, h) => s.ChildrenChanged -= h
            );

        private static readonly WeakEvent<IRootProvider, EventArgs> FocusChangeddWeakEvent = WeakEvent.Register<IRootProvider>(
            (s, h) => s.FocusChanged += h,
            (s, h) => s.FocusChanged -= h
        );

        private static readonly ConditionalWeakTable<AutomationPeer, AvnAutomationPeer> s_wrappers = new();
        private readonly WeakReference<AutomationPeer> _inner;

        private AvnAutomationPeer(AutomationPeer inner)
        {
            _inner = new WeakReference<AutomationPeer>(inner);
            ChildrenChangedWeakEvent.Subscribe(inner, this);
            if (inner is IRootProvider root)
                FocusChangeddWeakEvent.Subscribe(root, this);
        }

        public void OnEvent(object? sender, WeakEvent ev, EventArgs e)
        {
            if (ev == ChildrenChangedWeakEvent)
            {
                Node?.ChildrenChanged();
            }
            else if (ev == FocusChangeddWeakEvent)
            {
                Node?.FocusChanged();
            }
        }

        ~AvnAutomationPeer()
        {
            Node?.Dispose();
        }

        public IAvnAutomationNode? Node { get; private set; }
        public IAvnString? AcceleratorKey => this._inner.TryGetTarget(out var _inner) ? _inner.GetAcceleratorKey().ToAvnString() : null;

        public IAvnString? AccessKey => this._inner.TryGetTarget(out var _inner) ? _inner.GetAccessKey().ToAvnString() : default;
        public AvnAutomationControlType AutomationControlType => this._inner.TryGetTarget(out var _inner) ? (AvnAutomationControlType)_inner.GetAutomationControlType() : default;
        public IAvnString? AutomationId => this._inner.TryGetTarget(out var _inner) ? _inner.GetAutomationId().ToAvnString() : default;
        public AvnRect BoundingRectangle => this._inner.TryGetTarget(out var _inner) ? _inner.GetBoundingRectangle().ToAvnRect() : default;
        public IAvnAutomationPeerArray Children => new AvnAutomationPeerArray(this._inner.TryGetTarget(out var _inner) ? _inner.GetChildren() : Array.Empty<AutomationPeer>());
        public IAvnString ClassName => this._inner.TryGetTarget(out var _inner) ? _inner.GetClassName().ToAvnString() : default;
        public IAvnAutomationPeer? LabeledBy => this._inner.TryGetTarget(out var _inner) ? Wrap(_inner.GetLabeledBy()) : default;
        public IAvnString Name => this._inner.TryGetTarget(out var _inner) ? _inner.GetName().ToAvnString() : default;
        public IAvnAutomationPeer? Parent => this._inner.TryGetTarget(out var _inner) ? Wrap(_inner.GetParent()) : default;
        public IAvnAutomationPeer? VisualRoot => this._inner.TryGetTarget(out var _inner) ? Wrap(_inner.GetVisualRoot()) : default;

        public int HasKeyboardFocus() => this._inner.TryGetTarget(out var _inner) ? _inner.HasKeyboardFocus().AsComBool() : default;
        public int IsContentElement() => this._inner.TryGetTarget(out var _inner) ? _inner.IsContentElement().AsComBool() : default;
        public int IsControlElement() => this._inner.TryGetTarget(out var _inner) ? _inner.IsControlElement().AsComBool() : default;
        public int IsEnabled() => this._inner.TryGetTarget(out var _inner) ? _inner.IsEnabled().AsComBool() : default;
        public int IsKeyboardFocusable() => this._inner.TryGetTarget(out var _inner) ? _inner.IsKeyboardFocusable().AsComBool() : default;
        public void SetFocus()
        {
            if (this._inner.TryGetTarget(out var _inner))
                _inner.SetFocus();
        }

        public int ShowContextMenu() => this._inner.TryGetTarget(out var _inner) ? _inner.ShowContextMenu().AsComBool() : default;

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
                if (!this._inner.TryGetTarget(out var _inner))
                    return null;
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
            if (!this._inner.TryGetTarget(out var _inner))
                throw new InvalidOperationException("The peer has been disposed.");
            return _inner.GetProvider<T>() ?? throw new InvalidOperationException(
                $"The peer {_inner} does not implement {typeof(T)}.");
        }

        private int IsProvider<T>() => this._inner.TryGetTarget(out var _inner) ? (_inner.GetProvider<T>() is not null).AsComBool() : 0;
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
