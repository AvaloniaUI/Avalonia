using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers
{
    /// <summary>
    /// An automation peer which represents a <see cref="Control"/> element.
    /// </summary>
    public class ControlAutomationPeer : AutomationPeer
    {
        private IReadOnlyList<AutomationPeer>? _children;
        private bool _childrenValid;
        private AutomationPeer? _parent;
        private bool _parentValid;

        public ControlAutomationPeer(Control owner)
        {
            Owner = owner ?? throw new ArgumentNullException("owner");
            Initialize();
        }

        public Control Owner { get; }

        public AutomationPeer GetOrCreate(Control element)
        {
            if (element == Owner)
                return this;
            return CreatePeerForElement(element);
        }

        public static AutomationPeer CreatePeerForElement(Control element)
        {
            return element.GetOrCreateAutomationPeer();
        }

        protected override void BringIntoViewCore() => Owner.BringIntoView();

        protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore()
        {
            var children = _children ?? Array.Empty<AutomationPeer>();

            if (_childrenValid)
                return children;

            var newChildren = GetChildrenCore() ?? Array.Empty<AutomationPeer>();

            foreach (var peer in children.Except(newChildren))
                peer.TrySetParent(null);
            foreach (var peer in newChildren)
                peer.TrySetParent(this);

            _childrenValid = true;
            return _children = newChildren;
        }

        protected virtual IReadOnlyList<AutomationPeer>? GetChildrenCore()
        {
            var children = ((IVisual)Owner).VisualChildren;

            if (children.Count == 0)
                return null;

            var result = new List<AutomationPeer>();

            foreach (var child in children)
            {
                if (child is Control c && c.IsVisible)
                {
                    result.Add(GetOrCreate(c));
                }
            }

            return result;
        }

        protected override AutomationPeer? GetLabeledByCore()
        {
            var label = AutomationProperties.GetLabeledBy(Owner);
            return label is Control c ? GetOrCreate(c) : null;
        }

        protected override string? GetNameCore()
        {
            var result = AutomationProperties.GetName(Owner);

            if (string.IsNullOrWhiteSpace(result) && GetLabeledBy() is AutomationPeer labeledBy)
            {
                return labeledBy.GetName();
            }

            return null;
        }

        protected override AutomationPeer? GetParentCore()
        {
            EnsureConnected();
            return _parent;
        }

        /// <summary>
        /// Invalidates the peer's children and causes a re-read from <see cref="GetChildrenCore"/>.
        /// </summary>
        protected void InvalidateChildren()
        {
            _childrenValid = false;
            RaiseChildrenChangedEvent();
        }

        /// <summary>
        /// Invalidates the peer's parent.
        /// </summary>
        protected void InvalidateParent()
        {
            _parent = null;
            _parentValid = false;
        }

        protected override bool ShowContextMenuCore()
        {
            var c = Owner;

            while (c is object)
            {
                if (c.ContextMenu is object)
                {
                    c.ContextMenu.Open(c);
                    return true;
                }

                c = c.Parent as Control;
            }

            return false;
        }

        protected internal override bool TrySetParent(AutomationPeer? parent)
        {
            _parent = parent;
            return true;
        }

        protected override string? GetAcceleratorKeyCore() => AutomationProperties.GetAcceleratorKey(Owner);
        protected override string? GetAccessKeyCore() => AutomationProperties.GetAccessKey(Owner);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
        protected override string? GetAutomationIdCore() => AutomationProperties.GetAutomationId(Owner) ?? Owner.Name;
        protected override Rect GetBoundingRectangleCore() => GetBounds(Owner);
        protected override string GetClassNameCore() => Owner.GetType().Name;
        protected override bool HasKeyboardFocusCore() => Owner.IsFocused;
        protected override bool IsContentElementCore() => AutomationProperties.GetAccessibilityView(Owner) >= AccessibilityView.Content;
        protected override bool IsControlElementCore() => AutomationProperties.GetAccessibilityView(Owner) >= AccessibilityView.Control;
        protected override bool IsEnabledCore() => Owner.IsEnabled;
        protected override bool IsKeyboardFocusableCore() => Owner.Focusable;
        protected override void SetFocusCore() => Owner.Focus();

        protected override AutomationControlType GetControlTypeOverrideCore()
        {
            return AutomationProperties.GetControlTypeOverride(Owner) ?? GetAutomationControlTypeCore();
        }

        private static Rect GetBounds(Control control)
        {
            var root = control.GetVisualRoot();

            if (root is null)
                return default;

            var transform = control.TransformToVisual(root);

            if (!transform.HasValue)
                return default;

            return new Rect(control.Bounds.Size).TransformToAABB(transform.Value);
        }

        private void Initialize()
        {
            Owner.PropertyChanged += OwnerPropertyChanged;
            var visualChildren = ((IVisual)Owner).VisualChildren;
            visualChildren.CollectionChanged += VisualChildrenChanged;
        }

        private void VisualChildrenChanged(object? sender, EventArgs e) => InvalidateChildren();

        private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Visual.IsVisibleProperty)
            {
                var parent = Owner.GetVisualParent();
                if (parent is Control c)
                    (GetOrCreate(c) as ControlAutomationPeer)?.InvalidateChildren();
            }
            else if (e.Property == Visual.BoundsProperty || 
                     e.Property == Visual.RenderTransformProperty ||
                     e.Property == Visual.RenderTransformOriginProperty)
            {
                RaisePropertyChangedEvent(
                    AutomationElementIdentifiers.BoundingRectangleProperty,
                    null,
                    GetBounds(Owner));
            }
            else if (e.Property == Visual.VisualParentProperty)
            {
                InvalidateParent();
            }
        }


        private void EnsureConnected()
        {
            if (!_parentValid)
            {
                var parent = Owner.GetVisualParent();

                while (parent is object)
                {
                    if (parent is Control c)
                    {
                        var parentPeer = GetOrCreate(c);
                        parentPeer.GetChildren();
                    }

                    parent = parent.GetVisualParent();
                }

                _parentValid = true;
            }
        }
    }
}

