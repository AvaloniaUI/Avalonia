using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    public class AutomationPeerWrapper : UIAccessibilityElement, IUIAccessibilityContainer
    {
        private static readonly IReadOnlyDictionary<AutomationProperty, Action<AutomationPeerWrapper>> s_propertySetters =
            new Dictionary<AutomationProperty, Action<AutomationPeerWrapper>>()
            {
                { AutomationElementIdentifiers.NameProperty, UpdateName },
                { AutomationElementIdentifiers.HelpTextProperty, UpdateHelpText },
                { AutomationElementIdentifiers.BoundingRectangleProperty, UpdateBoundingRectangle },

                { RangeValuePatternIdentifiers.IsReadOnlyProperty, UpdateIsReadOnly },
                { RangeValuePatternIdentifiers.ValueProperty, UpdateValue },

                { ValuePatternIdentifiers.IsReadOnlyProperty, UpdateIsReadOnly },
                { ValuePatternIdentifiers.ValueProperty, UpdateValue },
            };

        private readonly AvaloniaView _view;
        private readonly AutomationPeerWrapper? _parent;

        private readonly AutomationPeer _peer;

        private readonly List<AutomationPeer?> _childrenList;
        private readonly Dictionary<AutomationPeer, AutomationPeerWrapper> _childrenMap;

        [Export("accessibilityContainerType")]
        public UIAccessibilityContainerType AccessibilityContainerType { get; set; }

        private AutomationPeerWrapper(AvaloniaView view, AutomationPeerWrapper parent, AutomationPeer peer) : base(parent)
        {
            _view = view;
            _parent = parent;

            _peer = peer;
            _peer.ChildrenChanged += PeerChildrenChanged;
            _peer.PropertyChanged += PeerPropertyChanged;

            _childrenList = new();
            _childrenMap = new();
        }

        public AutomationPeerWrapper(AvaloniaView view, AutomationPeer peer) : base(view)
        {
            _view = view;

            _peer = peer;
            _peer.ChildrenChanged += PeerChildrenChanged;
            _peer.PropertyChanged += PeerPropertyChanged;

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

        void UpdateChildren()
        {
            foreach (AutomationPeer child in _peer.GetChildren())
            {
                AutomationPeerWrapper? wrapper;
                if (!_childrenMap.TryGetValue(child, out wrapper) && !child.IsOffscreen())
                {
                    wrapper = new(_view, this, child);
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
            Rect bounds = peer.GetBoundingRectangle();
            PixelRect screenRect = new PixelRect(
                self._view.TopLevel.PointToScreen(bounds.TopLeft),
                self._view.TopLevel.PointToScreen(bounds.BottomRight)
                );
            CGRect nativeRect = new CGRect(
                screenRect.X, screenRect.Y,
                screenRect.Width, screenRect.Height
                );
            if (self.AccessibilityFrame != nativeRect)
            {
                self.AccessibilityFrame = nativeRect;
                UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, null);
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
                UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, null);
            }
        }

        private void PeerChildrenChanged(object? sender, EventArgs e)
        {
            UpdateChildren();
            UIAccessibility.PostNotification(UIAccessibilityPostNotification.ScreenChanged, null);
        }

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

            if (_peer.IsContentElement() && !_peer.IsOffscreen() &&
                (_peer.GetName().Length > 0 || _peer.IsKeyboardFocusable()))
            {
                AccessibilityContainerType = UIAccessibilityContainerType.None;
                IsAccessibilityElement = true;
            }
            else if (_peer.IsOffscreen())
            {
                _parent?._childrenList.Remove(_peer);
                _parent?._childrenMap.Remove(_peer);

                AccessibilityContainerType = UIAccessibilityContainerType.None;
                IsAccessibilityElement = false;
            }
            else
            {
                AccessibilityContainerType = UIAccessibilityContainerType.SemanticGroup;
                IsAccessibilityElement = false;
            }
        }

        public void UpdateAllProperties()
        {
            UpdateProperties(s_propertySetters.Keys.ToArray());
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
                        verticalAmount = ScrollAmount.SmallIncrement;
                        horizontalAmount = ScrollAmount.NoAmount;
                        didScroll = true;
                        break;
                    case UIAccessibilityScrollDirection.Down:
                        verticalAmount = ScrollAmount.SmallDecrement;
                        horizontalAmount = ScrollAmount.NoAmount;
                        didScroll = true;
                        break;
                    case UIAccessibilityScrollDirection.Left:
                        verticalAmount = ScrollAmount.NoAmount;
                        horizontalAmount = ScrollAmount.SmallIncrement;
                        didScroll = true;
                        break;
                    case UIAccessibilityScrollDirection.Right:
                        verticalAmount = ScrollAmount.NoAmount;
                        horizontalAmount = ScrollAmount.SmallDecrement;
                        didScroll = true;
                        break;
                    default:
                        verticalAmount = ScrollAmount.NoAmount;
                        horizontalAmount = ScrollAmount.NoAmount;
                        didScroll = false;
                        break;
                }

                scrollProvider.Scroll(verticalAmount, horizontalAmount);
                if (didScroll)
                {
                    UIAccessibility.PostNotification(UIAccessibilityPostNotification.PageScrolled, null);
                    return true;
                }
            }
            return false;
        }

        public static implicit operator AutomationPeer(AutomationPeerWrapper instance)
        {
            return instance._peer;
        }
    }
}
