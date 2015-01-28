// -----------------------------------------------------------------------
// <copyright file="TreeNode.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using ReactiveUI;

    internal class TreeNode : ReactiveObject
    {
        private string classes;

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

        public IReactiveDerivedList<TreeNode> Children
        {
            get;
            protected set;
        }

        public string Classes
        {
            get { return this.classes; }
            private set { this.RaiseAndSetIfChanged(ref this.classes, value); }
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
