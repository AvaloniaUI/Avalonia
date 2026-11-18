using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Input;
using Avalonia.Platform;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    internal sealed class AutomationPeerWrapper : UIAccessibilityElement, IUIAccessibilityContainer
    {
        private static readonly HashSet<AutomationControlType> s_containerTypes =
            new HashSet<AutomationControlType>()
            {
                AutomationControlType.Calendar,
                AutomationControlType.ComboBoxItem,
                AutomationControlType.Custom,
                AutomationControlType.DataGrid,
                AutomationControlType.DataItem,
                AutomationControlType.Document,
                AutomationControlType.Expander,
                AutomationControlType.Group,
                AutomationControlType.List,
                AutomationControlType.ListItem,
                AutomationControlType.Menu,
                AutomationControlType.MenuBar,
                AutomationControlType.MenuItem,
                AutomationControlType.Pane,
                AutomationControlType.ScrollViewer,
                AutomationControlType.SplitButton,
                AutomationControlType.Tab,
                AutomationControlType.TabItem,
                AutomationControlType.Table,
                AutomationControlType.TitleBar,
                AutomationControlType.ToolBar,
                AutomationControlType.Tree,
                AutomationControlType.TreeItem,
                AutomationControlType.Window,
            };


        private static readonly IReadOnlyDictionary<AutomationProperty, Action<AutomationPeerWrapper>> s_propertySetters =
            new Dictionary<AutomationProperty, Action<AutomationPeerWrapper>>()
            {
                { AutomationElementIdentifiers.AutomationIdProperty, UpdateAutomationId },
                { AutomationElementIdentifiers.NameProperty, UpdateName },
                { AutomationElementIdentifiers.HelpTextProperty, UpdateHelpText },
                { AutomationElementIdentifiers.BoundingRectangleProperty, UpdateBoundingRectangle },

                { RangeValuePatternIdentifiers.IsReadOnlyProperty, UpdateIsReadOnly },
                { RangeValuePatternIdentifiers.ValueProperty, UpdateValue },

                { ValuePatternIdentifiers.IsReadOnlyProperty, UpdateIsReadOnly },
                { ValuePatternIdentifiers.ValueProperty, UpdateValue },

                { SelectionItemPatternIdentifiers.IsSelectedProperty, UpdateSelected },
            };

        private readonly AvaloniaView _view;

        private readonly AutomationPeer _peer;

        private readonly AutomationPeerWrapper? _parent;

        private readonly List<NSObject> _childrenElements;

        private readonly Dictionary<AutomationPeer, AutomationPeerWrapper> _childrenMap;

        private readonly bool _isContainer;

        private AutomationPeerWrapper(AutomationPeerWrapper parent, AvaloniaView view, AutomationPeer peer) : base(parent)
        {
            _view = view;
            _parent = parent;

            _peer = peer;
            _peer.ChildrenChanged += PeerChildrenChanged;
            _peer.PropertyChanged += PeerPropertyChanged;

            AutomationControlType controlType = _peer.GetAutomationControlType();
            if (_isContainer = s_containerTypes.Contains(controlType))
            {
                AccessibilityContainerType = UIAccessibilityContainerType.SemanticGroup;
                IsAccessibilityElement = false;
            }

            _childrenElements = new();
            _childrenMap = new();
        }

        public AutomationPeerWrapper(AvaloniaView view, AutomationPeer peer) : base(view)
        {
            _view = view;
            _parent = null;

            _peer = peer;
            _peer.ChildrenChanged += PeerChildrenChanged;
            _peer.PropertyChanged += PeerPropertyChanged;

            AutomationControlType controlType = _peer.GetAutomationControlType();
            if (_isContainer = s_containerTypes.Contains(controlType))
            {
                AccessibilityContainerType = UIAccessibilityContainerType.SemanticGroup;
                IsAccessibilityElement = false;
            }

            _childrenElements = new();
            _childrenMap = new();
        }

        [Export("accessibilityElementCount")]
        public nint AccessibilityElementCount()
        {
            UpdateChildren();
            return _childrenElements.Count;
        }

        [Export("accessibilityElementAtIndex:")]
        public NSObject GetAccessibilityElementAt(nint index)
        {
            return index >= 0 && index < _childrenElements.Count ? _childrenElements[(int)index] : null!;
        }

        [Export("indexOfAccessibilityElement:")]
        public nint GetIndexOfAccessibilityElement(NSObject element)
        {
            int indexOf = _childrenElements.IndexOf(element);
            return indexOf < 0 ? NSRange.NotFound : indexOf;
        }

        // Without this list, VoiceOver can't swipe out of a native control's view; count/index alone aren't enough.
        [Export("accessibilityElements")]
        public NSObject GetAccessibilityElements()
        {
            UpdateChildren();
            return NSArray.FromNSObjects(_childrenElements.ToArray());
        }

        [Export("accessibilityContainerType")]
        public UIAccessibilityContainerType AccessibilityContainerType { get; set; }

        void UpdateChildren()
        {
            UpdateAllProperties();
            UpdateTraits();

            List<NSObject> elements = new();
            HashSet<AutomationPeer> retainedChildren = new();
            foreach (AutomationPeer child in _peer.GetChildren())
            {
                // A native control's peer throws from most members, so its view is listed instead of a wrapper.
                if (child is InteropAutomationPeer interop)
                {
                    if (FindNativeControlView(interop.NativeControlHandle) is { } nativeView)
                    {
                        elements.Add(nativeView);
                    }

                    continue;
                }

                if (child.IsOffscreen())
                {
                    continue;
                }

                if (!_childrenMap.TryGetValue(child, out AutomationPeerWrapper? wrapper))
                {
                    wrapper = new(this, _view, child);
                    _childrenMap.Add(child, wrapper);
                }

                elements.Add(wrapper);
                retainedChildren.Add(child);
                wrapper.UpdateAllProperties();
                wrapper.UpdateTraits();
            }

            foreach ((AutomationPeer child, AutomationPeerWrapper wrapper) in _childrenMap.ToArray())
            {
                if (!retainedChildren.Contains(child))
                {
                    _childrenMap.Remove(child);
                    wrapper.Dispose();
                }
            }

            _childrenElements.Clear();
            _childrenElements.AddRange(elements);
        }

        // Attached native views are subviews of the AvaloniaView; detached or hidden ones are not listed.
        private UIView? FindNativeControlView(IPlatformHandle handle)
        {
            if (handle.HandleDescriptor != UIViewControlHandle.UIViewDescriptor || handle.Handle == IntPtr.Zero)
            {
                return null;
            }

            foreach (UIView subview in _view.Subviews)
            {
                if (subview.Handle.Handle == handle.Handle)
                {
                    return subview.Hidden ? null : subview;
                }
            }

            return null;
        }

        private static void UpdateAutomationId(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            self.AccessibilityIdentifier = peer.GetAutomationId();
        }

        private static void UpdateName(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            self.AccessibilityLabel = peer.GetName();
        }

        private static void UpdateHelpText(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            self.AccessibilityHint = peer.GetHelpText();
        }

        private static void UpdateBoundingRectangle(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            InputElement? root = self._view.TopLevel.GetInputRoot()?.RootElement;
            Rect bounds = peer.GetBoundingRectangle();
            PixelRect screenRect = new PixelRect(
                root?.PointToScreen(bounds.TopLeft) ?? default,
                root?.PointToScreen(bounds.BottomRight) ?? default
                );
            CGRect nativeRect = new CGRect(
                screenRect.X, screenRect.Y,
                screenRect.Width, screenRect.Height
                );
            if (self.AccessibilityFrame != nativeRect)
            {
                self.AccessibilityFrame = nativeRect;
            }
        }

        private static void UpdateIsReadOnly(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            self.AccessibilityRespondsToUserInteraction =
                peer.IsEnabled() &&
                (peer.GetProvider<IValueProvider>()?.IsReadOnly == false ||
                 peer.GetProvider<IRangeValueProvider>()?.IsReadOnly == false ||
                 self.GetSelectionItemProvider() is not null ||
                 peer.GetProvider<IToggleProvider>() is not null ||
                 peer.GetProvider<IInvokeProvider>() is not null ||
                 peer.GetProvider<IScrollProvider>() is not null);

            self.AccessibilityTraits &= ~(ulong)UIAccessibilityTrait.Adjustable;
            if (peer.GetProvider<IRangeValueProvider>()?.IsReadOnly == false)
            {
                self.AccessibilityTraits |= (ulong)UIAccessibilityTrait.Adjustable;
            }
        }

        private static void UpdateValue(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            string? newValue =
                peer.GetProvider<IRangeValueProvider>()?.Value.ToString("0.##") ??
                peer.GetProvider<IValueProvider>()?.Value;
            if (self.AccessibilityValue != newValue)
            {
                self.AccessibilityValue = newValue;
            }
        }

        private static void UpdateSelected(AutomationPeerWrapper self)
        {
            self.AccessibilityTraits &= ~(ulong)UIAccessibilityTrait.Selected;
            if (self.GetSelectionItemProvider()?.IsSelected == true)
            {
                self.AccessibilityTraits |= (ulong)UIAccessibilityTrait.Selected;
            }
        }

        private void PeerChildrenChanged(object? sender, EventArgs e) =>
            UpdateChildren();

        private void PeerPropertyChanged(object? sender, AutomationPropertyChangedEventArgs e) =>
            UpdateProperties(e.Property);

        private void UpdateProperties(params AutomationProperty[] properties)
        {
            HashSet<Action<AutomationPeerWrapper>> calledSetters = new();
            foreach (AutomationProperty property in properties)
            {
                if (s_propertySetters.TryGetValue(property,
                    out Action<AutomationPeerWrapper>? setter) &&
                    !calledSetters.Contains(setter))
                {
                    calledSetters.Add(setter);
                    setter.Invoke(this);
                }
            }
        }

        public void UpdateAllProperties()
        {
            UpdateProperties(s_propertySetters.Keys.ToArray());
            bool isAccessibilityElement = !_peer.IsOffscreen() && _peer.IsControlElement();
            if (_isContainer)
            {
                bool isNamedSelectionItem =
                    _peer.GetProvider<ISelectionItemProvider>() is not null &&
                    !string.IsNullOrWhiteSpace(_peer.GetName());
                AccessibilityContainerType = isNamedSelectionItem ?
                    UIAccessibilityContainerType.None :
                    UIAccessibilityContainerType.SemanticGroup;
                IsAccessibilityElement = isNamedSelectionItem && isAccessibilityElement;
            }
            else
            {
                IsAccessibilityElement = isAccessibilityElement;
            }
        }

        public void UpdateTraits()
        {
            UIAccessibilityTrait traits = UIAccessibilityTrait.None;

            switch (_peer.GetAutomationControlType())
            {
                case AutomationControlType.Button:
                    traits |= UIAccessibilityTrait.Button;
                    break;
                case AutomationControlType.Header:
                    traits |= UIAccessibilityTrait.Header;
                    break;
                case AutomationControlType.Hyperlink:
                    traits |= UIAccessibilityTrait.Link;
                    break;
                case AutomationControlType.Image:
                    traits |= UIAccessibilityTrait.Image;
                    break;
            }

            if (_peer.IsEnabled() == false)
            {
                traits |= UIAccessibilityTrait.NotEnabled;
            }

            AccessibilityTraits = (ulong)traits;
            UpdateSelected(this);
        }

        [Export("accessibilityActivate")]
        public bool AccessibilityActivate()
        {
            ISelectionItemProvider? selectionItemProvider = _peer.GetProvider<ISelectionItemProvider>();
            IToggleProvider? toggleProvider = _peer.GetProvider<IToggleProvider>();
            IInvokeProvider? invokeProvider = _peer.GetProvider<IInvokeProvider>();
            if (selectionItemProvider is not null)
            {
                selectionItemProvider.Select();
                UpdateTraits();
                return true;
            }
            else if (toggleProvider is not null)
            {
                toggleProvider.Toggle();
                return true;
            }
            else if (invokeProvider is not null)
            {
                invokeProvider.Invoke();
                return true;
            }
            else if (_parent?.GetSelectionItemProvider() is { } parentSelectionItemProvider)
            {
                parentSelectionItemProvider.Select();
                UpdateTraits();
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool AccessibilityElementIsFocused()
        {
            base.AccessibilityElementIsFocused();
            return _peer.HasKeyboardFocus();
        }

        public override void AccessibilityElementDidBecomeFocused()
        {
            base.AccessibilityElementDidBecomeFocused();
            _peer.BringIntoView();
        }

        public override void AccessibilityDecrement()
        {
            base.AccessibilityDecrement();
            IRangeValueProvider? provider = _peer.GetProvider<IRangeValueProvider>();
            if (provider is not null)
            {
                double value = provider.Value;
                provider.SetValue(value - provider.SmallChange);
            }
        }

        public override void AccessibilityIncrement()
        {
            base.AccessibilityIncrement();
            IRangeValueProvider? provider = _peer.GetProvider<IRangeValueProvider>();
            if (provider is not null)
            {
                double value = provider.Value;
                provider.SetValue(value + provider.SmallChange);
            }
        }

        public override bool AccessibilityScroll(UIAccessibilityScrollDirection direction)
        {
            base.AccessibilityScroll(direction);
            IScrollProvider? scrollProvider = _peer.GetProvider<IScrollProvider>();
            if (scrollProvider is not null)
            {
                bool didScroll;
                ScrollAmount verticalAmount, horizontalAmount;
                switch (direction)
                {
                    case UIAccessibilityScrollDirection.Up:
                        verticalAmount = ScrollAmount.LargeIncrement;
                        horizontalAmount = ScrollAmount.NoAmount;
                        didScroll = true;
                        break;
                    case UIAccessibilityScrollDirection.Down:
                        verticalAmount = ScrollAmount.LargeDecrement;
                        horizontalAmount = ScrollAmount.NoAmount;
                        didScroll = true;
                        break;
                    case UIAccessibilityScrollDirection.Left:
                        verticalAmount = ScrollAmount.NoAmount;
                        horizontalAmount = ScrollAmount.LargeIncrement;
                        didScroll = true;
                        break;
                    case UIAccessibilityScrollDirection.Right:
                        verticalAmount = ScrollAmount.NoAmount;
                        horizontalAmount = ScrollAmount.LargeDecrement;
                        didScroll = true;
                        break;
                    default:
                        verticalAmount = ScrollAmount.NoAmount;
                        horizontalAmount = ScrollAmount.NoAmount;
                        didScroll = false;
                        break;
                }

                try
                {
                    scrollProvider.Scroll(horizontalAmount, verticalAmount);
                    if (didScroll)
                    {
                        UIAccessibility.PostNotification(UIAccessibilityPostNotification.PageScrolled, null);
                        return true;
                    }
                }
                catch (InvalidOperationException) { }
            }
            return false;
        }

        private ISelectionItemProvider? GetSelectionItemProvider()
        {
            for (AutomationPeerWrapper? wrapper = this; wrapper is not null; wrapper = wrapper._parent)
            {
                if (wrapper._peer.GetProvider<ISelectionItemProvider>() is { } provider)
                {
                    return provider;
                }
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _peer.ChildrenChanged -= PeerChildrenChanged;
                _peer.PropertyChanged -= PeerPropertyChanged;

                foreach (AutomationPeerWrapper child in _childrenMap.Values)
                {
                    child.Dispose();
                }

                _childrenElements.Clear();
                _childrenMap.Clear();
            }

            base.Dispose(disposing);
        }

        public static implicit operator AutomationPeer(AutomationPeerWrapper instance)
        {
            return instance._peer;
        }
    }
}
