// -----------------------------------------------------------------------
// <copyright file="Panel.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using Perspex.Collections;

    /// <summary>
    /// Base class for controls that can contain multiple children.
    /// </summary>
    public class Panel : Control, ILogical
    {
        private Controls children;

        public Controls Children
        {
            get
            {
                if (this.children == null)
                {
                    this.children = new Controls();
                    this.children.CollectionChanged += this.ChildrenChanged;
                }

                return this.children;
            }

            set
            {
                Contract.Requires<ArgumentNullException>(value != null);

                if (this.children != value)
                {
                    if (this.children != null)
                    {
                        this.ClearLogicalParent(this.children);
                        this.children.CollectionChanged -= this.ChildrenChanged;
                    }

                    this.children = value;
                    this.ClearVisualChildren();

                    if (this.children != null)
                    {
                        this.children.CollectionChanged += this.ChildrenChanged;
                        this.AddVisualChildren(value);
                        this.SetLogicalParent(value);
                        this.InvalidateMeasure();
                    }
                }
            }
        }

        public bool IsLogicalParent { get; set; } = true;

        IReadOnlyPerspexList<ILogical> ILogical.LogicalChildren
        {
            get { return this.children; }
        }

        private void ClearLogicalParent(IEnumerable<Control> controls)
        {
            if (this.IsLogicalParent)
            {
                foreach (var control in controls)
                {
                    control.Parent = null;
                }
            }
        }

        private void SetLogicalParent(IEnumerable<Control> controls)
        {
            if (this.IsLogicalParent)
            {
                foreach (var control in controls)
                {
                    control.Parent = this;
                }
            }
        }

        private void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // TODO: Handle Replace.
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.AddVisualChildren(e.NewItems.OfType<Visual>());
                    this.SetLogicalParent(e.NewItems.OfType<Control>());
                    break;

                case NotifyCollectionChangedAction.Remove:
                    this.ClearLogicalParent(e.OldItems.OfType<Control>());
                    this.RemoveVisualChildren(e.OldItems.OfType<Visual>());
                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.ClearLogicalParent(e.OldItems.OfType<Control>());
                    this.ClearVisualChildren();
                    this.AddVisualChildren(this.children);
                    break;
            }

            this.InvalidateMeasure();
        }
    }
}
