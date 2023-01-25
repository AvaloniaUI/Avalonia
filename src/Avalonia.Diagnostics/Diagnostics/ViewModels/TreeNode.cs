using System;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Reactive;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class TreeNode : ViewModelBase, IDisposable
    {
        private readonly IDisposable? _classesSubscription;
        private string _classes;
        private bool _isExpanded;

        protected TreeNode(AvaloniaObject avaloniaObject, TreeNode? parent, string? customName = null)
        {
            _classes = string.Empty;
            Parent = parent;
            var visual = avaloniaObject ;
            Type = customName ?? avaloniaObject.GetType().Name;
            Visual = visual!;
            FontWeight = IsRoot ? FontWeight.Bold : FontWeight.Normal;

            if (visual is Control control)
            {
                ElementName = control.Name;

                _classesSubscription = ((IObservable<object?>)control.Classes.GetWeakCollectionChangedObservable())
                    .StartWith(null)
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

        private bool IsRoot => Visual is TopLevel ||
                               Visual is ContextMenu ||
                               Visual is IPopupHost;

        public FontWeight FontWeight { get; }

        public abstract TreeNodeCollection Children
        {
            get;
        }

        public string Classes
        {
            get { return _classes; }
            private set { RaiseAndSetIfChanged(ref _classes, value); }
        }

        public string? ElementName
        {
            get;
        }

        public AvaloniaObject Visual
        {
            get;
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { RaiseAndSetIfChanged(ref _isExpanded, value); }
        }

        public TreeNode? Parent
        {
            get;
        }

        public string Type
        {
            get;
            private set;
        }

        public void Dispose()
        {
            _classesSubscription?.Dispose();
            Children.Dispose();
        }
    }
}
