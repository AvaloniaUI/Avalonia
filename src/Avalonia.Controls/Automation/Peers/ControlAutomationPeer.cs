using System;
using System.Collections.Generic;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls.Automation.Peers
{
    public abstract class ControlAutomationPeer : AutomationPeer
    {
        protected ControlAutomationPeer(Control owner)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public Control Owner { get; }

        public static AutomationPeer? GetOrCreatePeer(Control element)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));
            return element.GetOrCreateAutomationPeer();
        }

        protected override IAutomationPeerImpl CreatePlatformImplCore()
        {
            var root = Owner.GetVisualRoot();

            if (root is null)
                throw new InvalidOperationException("Cannot create automation peer for non-rooted control.");

            if ((root as TopLevel)?.PlatformImpl is IPlatformAutomationPeerFactory factory)
            {
                return factory.CreateAutomationPeerImpl(this);
            }
            
            throw new InvalidOperationException("UI automation not available on this platform.");
        }

        protected override Rect GetBoundingRectangleCore()
        {
            var root = Owner.GetVisualRoot();

            if (root is null)
                return Rect.Empty;

            var t = Owner.TransformToVisual(root);

            if (!t.HasValue)
                return Rect.Empty;

            return Owner.Bounds.TransformToAABB(t.Value);
        }

        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => GetChildren(Owner);
        protected override string GetClassNameCore() => Owner.GetType().Name;
        protected override string? GetNameCore() => AutomationProperties.GetName(Owner);

        protected override AutomationPeer? GetParentCore()
        {
            var parent = Owner.Parent;

            while (parent is object)
            {
                if (parent is Control controlParent)
                {
                    var result = GetOrCreatePeer(controlParent);

                    if (result is object)
                        return result;
                }

                parent = parent.Parent;
            }

            throw new InvalidOperationException("Cannot find parent automation peer.");
        }

        protected override bool IsKeyboardFocusableCore() => Owner.Focusable;
        protected override void SetFocusCore() => Owner.Focus();

        protected static IReadOnlyList<AutomationPeer>? GetChildren(IVisual control)
        {
            List<AutomationPeer>? children = null;

            static void Iterate(IVisual parent, ref List<AutomationPeer>? result)
            {
                foreach (var child in parent.VisualChildren)
                {
                    AutomationPeer? peer = null;

                    if (child is Control control)
                        peer = GetOrCreatePeer(control);

                    if (peer is object)
                    {
                        result ??= new List<AutomationPeer>();
                        result.Add(peer);
                    }
                    else
                    {
                        Iterate(child, ref result);
                    }
                }
            }

            Iterate(control, ref children);
            return children;
        }
    }
}

