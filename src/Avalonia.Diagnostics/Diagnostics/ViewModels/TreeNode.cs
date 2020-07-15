using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class TreeNode : ViewModelBase, IDisposable
    {
        private IDisposable _classesSubscription;
        private string _classes;
        private bool _isExpanded;

        public TreeNode(IVisual visual, TreeNode parent)
        {
            Parent = parent;
            Type = visual.GetType().Name;
            Visual = visual;

            if (visual is IControl control)
            {
                var removed = Observable.FromEventPattern<LogicalTreeAttachmentEventArgs>(
                    x => control.DetachedFromLogicalTree += x,
                    x => control.DetachedFromLogicalTree -= x);
                var classesChanged = Observable.FromEventPattern<
                        NotifyCollectionChangedEventHandler,
                        NotifyCollectionChangedEventArgs>(
                        x => control.Classes.CollectionChanged += x,
                        x => control.Classes.CollectionChanged -= x)
                    .TakeUntil(removed);

                _classesSubscription = classesChanged.Select(_ => Unit.Default)
                    .StartWith(Unit.Default)
                    .Subscribe(_ =>
                    {
                        if (control.Classes.Count > 0)
                        {
                            Classes = "(" + string.Join(" ", control.Classes) + ")";
                        }
                        else
                        {
                            Classes = string.Empty;
                        }
                    });
            }
        }

        public TreeNodeCollection Children
        {
            get;
            protected set;
        }

        public string Classes
        {
            get { return _classes; }
            private set { RaiseAndSetIfChanged(ref _classes, value); }
        }

        public IVisual Visual
        {
            get;
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { RaiseAndSetIfChanged(ref _isExpanded, value); }
        }

        public TreeNode Parent
        {
            get;
        }

        public string Type
        {
            get;
            private set;
        }

        public IndexPath Index
        {
            get
            {
                var indices = new List<int>();
                var child = this;
                var parent = Parent;
                
                while (parent is object)
                {
                    indices.Add(IndexOf(parent.Children, child));
                    child = child.Parent;
                    parent = parent.Parent;
                }

                indices.Add(0);
                indices.Reverse();
                return new IndexPath(indices);
            }
        }

        public void Dispose()
        {
            _classesSubscription.Dispose();
            Children.Dispose();
        }

        private static int IndexOf(IReadOnlyList<TreeNode> collection, TreeNode item)
        {
            var count = collection.Count;

            for (var i = 0; i < count; ++i)
            {
                if (collection[i] == item)
                {
                    return i;
                }
            }

            throw new AvaloniaInternalException("TreeNode was not present in parent Children collection.");
        }
    }
}
