// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Styling;
using ReactiveUI;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class TreeNode : ReactiveObject
    {
        private string _classes;
        private bool _isExpanded;

        public TreeNode(Control control, TreeNode parent)
        {
            Control = control;
            Parent = parent;
            Type = control.GetType().Name;

            var classesChanged = Observable.FromEventPattern<
                    NotifyCollectionChangedEventHandler, 
                    NotifyCollectionChangedEventArgs>(
                x => control.Classes.CollectionChanged += x,
                x => control.Classes.CollectionChanged -= x)
                .TakeUntil(((IStyleable)control).StyleDetach);

            classesChanged.Select(_ => Unit.Default)
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

        public IReadOnlyReactiveList<TreeNode> Children
        {
            get;
            protected set;
        }

        public string Classes
        {
            get { return _classes; }
            private set { this.RaiseAndSetIfChanged(ref _classes, value); }
        }

        public Control Control
        {
            get;
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { this.RaiseAndSetIfChanged(ref _isExpanded, value); }
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
    }
}
