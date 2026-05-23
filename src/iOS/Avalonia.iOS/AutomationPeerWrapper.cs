using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Input;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    public class AutomationPeerWrapper : UIAccessibilityElement, IUIAccessibilityContainer
    {
        private static readonly ImmutableHashSet<AutomationControlType> s_containerTypes =
        [
            AutomationControlType.Calendar,
            AutomationControlType.ComboBoxItem,
            AutomationControlType.Custom,
            AutomationControlType.DataGrid,
            AutomationControlType.DataItem,
            AutomationControlType.Document,
            AutomationControlType.Expander,
            AutomationControlType.Group,
            AutomationControlType.Header,
            AutomationControlType.HeaderItem,
            AutomationControlType.List,
            AutomationControlType.ListItem,
            AutomationControlType.Menu,
            AutomationControlType.MenuBar,
            AutomationControlType.MenuItem,
            AutomationControlType.Pane,
            AutomationControlType.SplitButton,
            AutomationControlType.Tab,
            AutomationControlType.TabItem,
            AutomationControlType.Table,
            AutomationControlType.TitleBar,
            AutomationControlType.ToolBar,
            AutomationControlType.Tree,
            AutomationControlType.TreeItem,
            AutomationControlType.Window,
        ];


        private static readonly ImmutableDictionary<AutomationProperty, Action<AutomationPeerWrapper>> s_propertySetters =
        [
            new(AutomationElementIdentifiers.NameProperty, UpdateName),
            new(AutomationElementIdentifiers.HelpTextProperty, UpdateHelpText),
            new(AutomationElementIdentifiers.BoundingRectangleProperty, UpdateBoundingRectangle),

            new(RangeValuePatternIdentifiers.IsReadOnlyProperty, UpdateIsReadOnly),
            new(RangeValuePatternIdentifiers.ValueProperty, UpdateValue),

            new(ValuePatternIdentifiers.IsReadOnlyProperty, UpdateIsReadOnly),
            new(ValuePatternIdentifiers.ValueProperty, UpdateValue),
        ];

        private readonly AvaloniaView _view;

        private readonly AutomationPeer _peer;

        private readonly List<AutomationPeer?> _childrenList;

        private readonly Dictionary<AutomationPeer, AutomationPeerWrapper> _childrenMap;

        private readonly bool _isContainer;

        private AutomationPeerWrapper(AutomationPeerWrapper parent, AvaloniaView view, AutomationPeer peer) : base(parent)
        {
            _view = view;

            _peer = peer;
            _peer.ChildrenChanged += PeerChildrenChanged;
            _peer.PropertyChanged += PeerPropertyChanged;

            AutomationControlType controlType = _peer.GetAutomationControlType();
            if (_isContainer = s_containerTypes.Contains(controlType))
            {
                AccessibilityContainerType = UIAccessibilityContainerType.SemanticGroup;
            }

            _childrenList = new();
            _childrenMap = new();
        }

        public AutomationPeerWrapper(AvaloniaView view, AutomationPeer peer) : base(view)
        {
            _view = view;

            _peer = peer;
            _peer.ChildrenChanged += PeerChildrenChanged;
            _peer.PropertyChanged += PeerPropertyChanged;

            AutomationControlType controlType = _peer.GetAutomationControlType();
            if (_isContainer = s_containerTypes.Contains(controlType))
            {
                AccessibilityContainerType = UIAccessibilityContainerType.SemanticGroup;
            }

            _childrenList = new();
            _childrenMap = new();
        }

        [Export("accessibilityElementCount")]
        public nint AccessibilityElementCount()
        {
            UpdateChildren();
            return _childrenList.Count;
        }

        [Export("accessibilityElementAtIndex:")]
        public NSObject GetAccessibilityElementAt(nint index)
        {
            AutomationPeer? child = _childrenList[(int)index];
            if (child is not null)
            {
                return _childrenMap[child];
            }
            else
            {
                throw new ArgumentNullException();
            }
        }

        [Export("indexOfAccessibilityElement:")]
        public nint GetIndexOfAccessibilityElement(NSObject element)
        {
            int indexOf = _childrenList.IndexOf((element as AutomationPeerWrapper)?._peer);
            return indexOf < 0 ? NSRange.NotFound : indexOf;
        }

        [Export("accessibilityContainerType")]
        public UIAccessibilityContainerType AccessibilityContainerType { get; set; }

        void UpdateChildren()
        {
            UpdateAllProperties();
            UpdateTraits();

            foreach (AutomationPeer child in _peer.GetChildren())
            {
                AutomationPeerWrapper? wrapper;
                if (child.IsOffscreen())
                {
                    _childrenList.Remove(child);
                    _childrenMap.Remove(child);
                    continue;
                }
                else if (!_childrenMap.TryGetValue(child, out wrapper))
                {
                    wrapper = new(this, _view, child);
                    _childrenList.Add(child);
                    _childrenMap.Add(child, wrapper);
                }

                wrapper?.UpdateAllProperties();
                wrapper?.UpdateTraits();
            }
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
                UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, self);
            }
        }

        private static void UpdateIsReadOnly(AutomationPeerWrapper self)
        {
            AutomationPeer peer = self;
            self.AccessibilityRespondsToUserInteraction =
                peer.GetProvider<IValueProvider>()?.IsReadOnly ??
                peer.GetProvider<IRangeValueProvider>()?.IsReadOnly ??
                peer.IsEnabled();
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
                UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, (NSString?)newValue);
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
            IsAccessibilityElement = !_isContainer &&
                !_peer.IsOffscreen() && _peer.IsControlElement();
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

            if (_peer.GetProvider<IRangeValueProvider>()?.IsReadOnly == false)
            {
                traits |= UIAccessibilityTrait.Adjustable;
            }

            if (_peer.GetProvider<ISelectionItemProvider>()?.IsSelected == true)
            {
                traits |= UIAccessibilityTrait.Selected;
            }

            if (_peer.GetProvider<IValueProvider>()?.IsReadOnly == false)
            {
                traits |= UIAccessibilityTrait.UpdatesFrequently;
            }

            if (_peer.IsEnabled() == false)
            {
                traits |= UIAccessibilityTrait.NotEnabled;
            }

            AccessibilityTraits = (ulong)traits;
        }

        [Export("accessibilityActivate")]
        public bool AccessibilityActivate()
        {
            IToggleProvider? toggleProvider = _peer.GetProvider<IToggleProvider>();
            IInvokeProvider? invokeProvider = _peer.GetProvider<IInvokeProvider>();
            if (toggleProvider is not null)
            {
                toggleProvider.Toggle();
                return true;
            }
            else if (invokeProvider is not null)
            {
                invokeProvider.Invoke();
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
                    scrollProvider.Scroll(verticalAmount, horizontalAmount);
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

        public static implicit operator AutomationPeer(AutomationPeerWrapper instance)
        {
            return instance._peer;
        }
    }
}
