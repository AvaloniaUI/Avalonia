// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;
using Perspex.Controls;
using ReactiveUI;

namespace Perspex.Diagnostics.ViewModels
{
    internal class TreeNode : ReactiveObject
    {
        private string _classes;
        private bool _isExpanded = true;

        public TreeNode(Control control)
        {
            Control = control;
            Type = control.GetType().Name;

            var classesChanged = Observable.FromEventPattern<
                    NotifyCollectionChangedEventHandler, 
                    NotifyCollectionChangedEventArgs>(
                x => control.Classes.CollectionChanged += x,
                x => control.Classes.CollectionChanged -= x);

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

        public bool IsExpanded
        {
            get { return _isExpanded; }
            private set { this.RaiseAndSetIfChanged(ref _isExpanded, value); }
        }

        public string Type
        {
            get;
            private set;
        }

        public Control Control
        {
            get;
            private set;
        }
    }
}
