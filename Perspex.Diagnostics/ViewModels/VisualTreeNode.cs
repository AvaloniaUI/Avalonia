// -----------------------------------------------------------------------
// <copyright file="VisualTreeNode.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using System;
    using System.Reactive;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using Perspex.Styling;
    using ReactiveUI;

    internal class VisualTreeNode : ReactiveObject
    {
        private string classes;

        public VisualTreeNode(IVisual visual)
        {
            this.Children = visual.VisualChildren.CreateDerivedCollection(x => new VisualTreeNode(x));
            this.Type = visual.GetType().Name;
            this.Visual = visual;

            Control control = visual as Control;

            if (control != null)
            {
                this.IsInTemplate = control.TemplatedParent != null;

                control.Classes.Changed.Select(_ => Unit.Default).StartWith(Unit.Default).Subscribe(_ =>
                {
                    if (control.Classes.Count > 0)
                    {
                        this.Classes = "(" + string.Join(" ", control.Classes) + ")";
                    }
                    else
                    {
                        this.Classes = "";
                    }
                });
            }
        }

        public IReactiveDerivedList<VisualTreeNode> Children { get; private set; }

        public string Classes
        {
            get { return this.classes; }
            private set { this.RaiseAndSetIfChanged(ref this.classes, value); }
        }

        public bool IsInTemplate { get; private set; }

        public string Type { get; private set; }

        public IVisual Visual { get; private set; }
    }
}
