using System;
using System.Collections.Generic;
using Avalonia.Controls.Platform;
using Avalonia.Platform;

#nullable enable

namespace Avalonia.Controls.Automation.Peers
{
    /// <summary>
    /// Provides a base class that exposes an element to UI Automation.
    /// </summary>
    public abstract class AutomationPeer
    {
        private AutomationPeer? _parent;
        private IReadOnlyList<AutomationPeer>? _children;
        private bool _parentValid;
        private bool _childrenValid;

        /// <summary>
        /// Gets the platform implementation of the automation peer.
        /// </summary>
        public IAutomationPeerImpl? PlatformImpl { get; private set; }

        /// <summary>
        /// Gets the bounding rectangle of the element that is associated with the automation peer
        /// in top-level coordinates.
        /// </summary>
        public Rect GetBoundingRectangle() => GetBoundingRectangleCore();

        /// <summary>
        /// Gets the collection of elements that are represented in the UI Automation tree as
        /// immediate child elements of the automation peer.
        /// </summary>
        public IReadOnlyList<AutomationPeer> GetChildren()
        {
            if (!_childrenValid)
            {
                _children = GetChildrenCore();
                _childrenValid = true;
            }

            return _children ?? Array.Empty<AutomationPeer>();
        }

        /// <summary>
        /// Gets a string that describes the class of the element.
        /// </summary>
        public string GetClassName() => GetClassNameCore() ?? string.Empty;

        /// <summary>
        /// Gets text that describes the element that is associated with this automation peer.
        /// </summary>
        public string GetName() => GetNameCore() ?? string.Empty;

        public AutomationPeer? GetParent()
        {
            if (!_parentValid)
            {
                _parent = GetParentCore();
                _parentValid = true;
            }

            return _parent;
        }

        public AutomationPeer? GetPeerFromPoint(Point point) => GetPeerFromPointCore(point);

        public Rect GetVisibleBoundingRect() => GetVisibleBoundingRectCore();

        public bool IsKeyboardFocusable() => IsKeyboardFocusableCore();

        public void SetFocus() => SetFocusCore();

        protected abstract Rect GetBoundingRectangleCore();
        protected abstract IReadOnlyList<AutomationPeer>? GetChildrenCore();
        protected abstract string GetClassNameCore();
        protected abstract string? GetNameCore();
        protected abstract AutomationPeer? GetParentCore();
        protected abstract bool IsKeyboardFocusableCore();
        protected abstract void SetFocusCore();

        protected virtual AutomationPeer? GetPeerFromPointCore(Point point)
        {
            AutomationPeer? found = null;

            foreach (var child in GetChildren())
            {
                found = child.GetPeerFromPoint(point);

                if (found is object)
                    break;
            }

            if (found is null)
            {
                var bounds = GetVisibleBoundingRect();

                if (bounds.Contains(point))
                    found = this;
            }

            return found;
        }

        protected virtual Rect GetVisibleBoundingRectCore() => GetBoundingRectangleCore();

        internal void CreatePlatformImpl()
        {
            var ifs = AvaloniaLocator.Current.GetService<IPlatformAutomationInterface>();

            if (ifs is null)
            {
                throw new NotSupportedException("No automation interface registered for this platform.");
            }

            PlatformImpl = ifs.CreateAutomationPeerImpl(this);
        }
    }
}
