// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;
using Perspex.Controls;
using ReactiveUI;

namespace Perspex.Diagnostics.ViewModels
{
    internal class TreeNode : ReactiveObject
    {
        private string _classes;

        public TreeNode(Control control)
        {
            this.Control = control;
            this.Type = control.GetType().Name;

            control.Classes.Changed.Select(_ => Unit.Default)
                .StartWith(Unit.Default)
                .Subscribe(_ =>
                {
                    if (control.Classes.Count > 0)
                    {
                        this.Classes = "(" + string.Join(" ", control.Classes) + ")";
                    }
                    else
                    {
                        this.Classes = string.Empty;
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
