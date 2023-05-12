using System;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;
using System.Linq;
using Avalonia.Reactive;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class LogicalTreeNode : TreeNode
    {
        public LogicalTreeNode(AvaloniaObject avaloniaObject, TreeNode? parent)
            : base(avaloniaObject, parent)
        {
            Children =  avaloniaObject switch
            {
                ILogical logical => new LogicalTreeNodeCollection(this, logical),
                Controls.TopLevelGroup host => new TopLevelGroupHostLogical(this, host),
                _ => TreeNodeCollection.Empty
            };
        }

        public override TreeNodeCollection Children { get; }

        public static LogicalTreeNode[] Create(object control)
        {
            var logical = control as AvaloniaObject;
            return logical != null ? new[] { new LogicalTreeNode(logical, null) } : Array.Empty<LogicalTreeNode>();
        }

        internal class LogicalTreeNodeCollection : TreeNodeCollection
        {
            private readonly ILogical _control;
            private IDisposable? _subscription;

            public LogicalTreeNodeCollection(TreeNode owner, ILogical control)
                : base(owner)
            {
                _control = control;
            }

            public override void Dispose()
            {
                base.Dispose();
                _subscription?.Dispose();
            }

            protected override void Initialize(AvaloniaList<TreeNode> nodes)
            {
                _subscription = _control.LogicalChildren.ForEachItem(
                    (i, item) => nodes.Insert(i, new LogicalTreeNode((AvaloniaObject)item, Owner)),
                    (i, item) => nodes.RemoveAt(i),
                    () => nodes.Clear());
            }
        }

        internal class TopLevelGroupHostLogical : TreeNodeCollection
        {
            private readonly Controls.TopLevelGroup _group;
            private readonly CompositeDisposable _subscriptions = new(1);

            public TopLevelGroupHostLogical(TreeNode owner, Controls.TopLevelGroup host) :
                base(owner)
            {
                _group = host;
            }

            protected override void Initialize(AvaloniaList<TreeNode> nodes)
            {
                for (var i = 0; i < _group.Items.Count; i++)
                {
                    var window = _group.Items[i];
                    if (window is Views.MainWindow)
                    {
                        continue;
                    }
                    nodes.Add(new LogicalTreeNode(window, Owner));
                }
                void GroupOnAdded(object? sender, TopLevel e)
                {
                    if (e is Views.MainWindow)
                    {
                        return;
                    }

                    nodes.Add(new LogicalTreeNode(e, Owner));
                }
                void GroupOnRemoved(object? sender, TopLevel e)
                {
                    if (e is Views.MainWindow)
                    {
                        return;
                    }

                    nodes.Add(new LogicalTreeNode(e, Owner));
                }
                
                _group.Added += GroupOnAdded;
                _group.Removed += GroupOnRemoved;

                _subscriptions.Add(new Disposable.AnonymousDisposable(() =>
                {
                    _group.Added -= GroupOnAdded;
                    _group.Removed -= GroupOnRemoved;
                }));
            }

            public override void Dispose()
            {
                _subscriptions?.Dispose();
                base.Dispose();
            }
        }
    }
}
