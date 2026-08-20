using System;
using System.Collections.Generic;

using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Avalonia.iOS
{
    // Bridges Avalonia's AutomationPeer tree into the UIKit accessibility tree so assistive
    // technologies and UI automation tools (VoiceOver, Appium / XCUITest) can see and drive
    // Avalonia controls. Each exposed element is keyed by accessibilityIdentifier = AutomationId,
    // mirroring the Android bridge's use of the view-id resource name.
    public partial class AvaloniaView
    {
        private NSObject[]? _accessibilityElements;
        private bool _accessibilityHooked;

        private void EnsureAccessibilityElements()
        {
            if (_accessibilityElements is not null)
            {
                return;
            }

            // The host view is a container, not an accessibility element itself.
            base.IsAccessibilityElement = false;

            AutomationPeer rootPeer = ControlAutomationPeer.CreatePeerForElement(_topLevel);

            if (!_accessibilityHooked)
            {
                rootPeer.ChildrenChanged += (_, _) => this.InvalidateAccessibilityElements();
                _accessibilityHooked = true;
            }

            List<NSObject> elements = new();
            CollectAccessibilityElements(rootPeer, elements);
            _accessibilityElements = elements.ToArray();
        }

        private void CollectAccessibilityElements(AutomationPeer peer, List<NSObject> elements)
        {
            foreach (AutomationPeer child in peer.GetChildren())
            {
                if (child.IsContentElement() && !child.IsOffscreen())
                {
                    elements.Add(new AvaloniaAccessibilityElement(this, child));
                }

                CollectAccessibilityElements(child, elements);
            }
        }

        internal void InvalidateAccessibilityElements()
        {
            _accessibilityElements = null;
            UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, null);
        }

        internal CGRect AccessibilityFrameForPeer(AutomationPeer peer)
        {
            Rect bounds = peer.GetBoundingRectangle();

            // AutomationPeer bounds are in top-level (logical) coordinates, which map 1:1 to this
            // UIView's point coordinate space; convert that view-space rect to screen coordinates.
            CGRect rectInView = new((nfloat)bounds.X, (nfloat)bounds.Y, (nfloat)bounds.Width, (nfloat)bounds.Height);
            return UIAccessibility.ConvertFrameToScreenCoordinates(rectInView, this);
        }

        [Export("accessibilityElementCount")]
        public nint AccessibilityElementCount()
        {
            this.EnsureAccessibilityElements();
            return _accessibilityElements!.Length;
        }

        [Export("accessibilityElementAtIndex:")]
        public NSObject? GetAccessibilityElementAt(nint index)
        {
            this.EnsureAccessibilityElements();

            if (index < 0 || index >= _accessibilityElements!.Length)
            {
                return null;
            }

            return _accessibilityElements[index];
        }

        [Export("indexOfAccessibilityElement:")]
        public nint IndexOfAccessibilityElement(NSObject element)
        {
            this.EnsureAccessibilityElements();
            return Array.IndexOf(_accessibilityElements!, element);
        }
    }

    internal sealed class AvaloniaAccessibilityElement : UIAccessibilityElement
    {
        private readonly AvaloniaView _container;
        private readonly AutomationPeer _peer;

        public AvaloniaAccessibilityElement(AvaloniaView container, AutomationPeer peer)
            : base(container)
        {
            _container = container;
            _peer = peer;

            this.IsAccessibilityElement = true;

            // Keep the exposed accessibility properties in sync with the live control state so that
            // automation oracles (and VoiceOver) read current values, not a snapshot taken at creation.
            this.RefreshFromPeer();
            peer.PropertyChanged += (_, _) => this.RefreshFromPeer();
        }

        private void RefreshFromPeer()
        {
            string? automationId = _peer.GetAutomationId();
            this.AccessibilityIdentifier = string.IsNullOrEmpty(automationId) ? null : automationId;

            string name = _peer.GetName();
            this.AccessibilityLabel = string.IsNullOrEmpty(name) ? null : name;

            this.AccessibilityFrame = _container.AccessibilityFrameForPeer(_peer);
            this.AccessibilityTraits = (ulong)TraitsForPeer(_peer);

            string? value = _peer.GetProvider<IValueProvider>()?.Value;
            this.AccessibilityValue = string.IsNullOrEmpty(value) ? null : value;
        }

        [Export("accessibilityActivate")]
        public bool AccessibilityActivate()
        {
            if (_peer.GetProvider<IInvokeProvider>() is { } invoke)
            {
                invoke.Invoke();
                return true;
            }

            if (_peer.GetProvider<IToggleProvider>() is { } toggle)
            {
                toggle.Toggle();
                return true;
            }

            return false;
        }

        private static UIAccessibilityTrait TraitsForPeer(AutomationPeer peer)
        {
            UIAccessibilityTrait traits = UIAccessibilityTrait.None;

            if (peer.GetProvider<IInvokeProvider>() is not null)
            {
                traits |= UIAccessibilityTrait.Button;
            }

            if (!peer.IsEnabled())
            {
                traits |= UIAccessibilityTrait.NotEnabled;
            }

            return traits;
        }
    }
}
