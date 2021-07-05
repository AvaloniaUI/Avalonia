using System;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class VisualTreeNode : TreeNode
    {
        public VisualTreeNode(IVisual visual, TreeNode? parent)
            : base(visual, parent)
        {
            Children = new VisualTreeNodeCollection(this, visual);

            if ((Visual is IStyleable styleable))
            {
                IsInTemplate = styleable.TemplatedParent != null;
            }
        }

        public bool IsInTemplate { get; private set; }

        public override TreeNodeCollection Children { get; }

        public static VisualTreeNode[] Create(object control)
        {
            return control is IVisual visual ? new[] { new VisualTreeNode(visual, null) } : Array.Empty<VisualTreeNode>();
        }

        internal class VisualTreeNodeCollection : TreeNodeCollection
        {
            private readonly IVisual _control;
            private IDisposable? _subscription;

            public VisualTreeNodeCollection(TreeNode owner, IVisual control)
                : base(owner)
            {
                _control = control;
            }

            public override void Dispose()
            {
                _subscription?.Dispose();
            }

            protected override void Initialize(AvaloniaList<TreeNode> nodes)
            {
                if (_control is Popup p)
                {
                    _subscription = p.GetObservable(Popup.ChildProperty).Subscribe(child =>
                    {
                        if (child != null)
                        {
                            nodes.Add(new VisualTreeNode(child, Owner));
                        }
                        else
                        {
                            nodes.Clear();
                        }

                    });
                }
                else
                {
                    _subscription = _control.VisualChildren.ForEachItem(
                        (i, item) => nodes.Insert(i, new VisualTreeNode(item, Owner)),
                        (i, item) => nodes.RemoveAt(i),
                        () => nodes.Clear());
                }
            }
        }
    }
}
