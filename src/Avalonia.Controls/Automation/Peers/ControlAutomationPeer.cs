using System;
using System.Collections.Generic;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls.Automation.Peers
{
    public abstract class ControlAutomationPeer : AutomationPeer
    {
        public ControlAutomationPeer(Control owner)
        {
            Owner = owner ?? throw new ArgumentNullException("owner");
        }

        public Control Owner { get; }

        public static AutomationPeer? GetOrCreatePeer(Control element)
        {
            element = element ?? throw new ArgumentNullException("element");
            return element.GetOrCreateAutomationPeer();
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

