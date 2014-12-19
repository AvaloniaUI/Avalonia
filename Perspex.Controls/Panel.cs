// -----------------------------------------------------------------------
// <copyright file="Panel.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
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
                        this.children.CollectionChanged -= this.ChildrenChanged;
                    }

                    this.children = value;
                    this.ClearVisualChildren();

                    if (this.children != null)
                    {
                        this.children.CollectionChanged += this.ChildrenChanged;
                        this.AddVisualChildren(value);
                        this.InvalidateMeasure();
                    }
                }
            }
        }

        IReadOnlyPerspexList<ILogical> ILogical.LogicalChildren
        {
            get { return this.children; }
        }

        private void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // TODO: Handle Move and Replace.
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.AddVisualChildren(e.NewItems.OfType<Visual>());

                    foreach (var child in e.NewItems.OfType<Control>())
                    {
                        child.Parent = this;
                    }

                    break;

                case NotifyCollectionChangedAction.Remove:
                    this.RemoveVisualChildren(e.OldItems.OfType<Visual>());

                    foreach (var child in e.OldItems.OfType<Control>())
                    {
                        child.Parent = null;
                    }

                    break;

                case NotifyCollectionChangedAction.Reset:
                    this.ClearVisualChildren();
                    this.AddVisualChildren(this.children);
                    break;
            }

            this.InvalidateMeasure();
        }
    }
}
