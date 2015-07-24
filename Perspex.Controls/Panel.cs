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
    /// <remarks>
    /// Controls can be added to a <see cref="Panel"/> by adding them to its <see cref="Children"/>
    /// collection. All children are layed out to fill the panel.
    /// </remarks>
    public class Panel : Control, ILogical, IItemsPanel
    {
        private Controls children;

        private ILogical childLogicalParent;

        /// <summary>
        /// Initializes a new instance of the <see cref="Panel"/> class.
        /// </summary>
        public Panel()
        {
            this.childLogicalParent = this;
        }

        /// <summary>
        /// Gets or sets the children of the <see cref="Panel"/>.
        /// </summary>
        /// <remarks>
        /// Even though this property can be set, the setter is only intended for use in object
        /// initializers. Assigning to this property does not change the underlying collection,
        /// it simply clears the existing collection and addds the contents of the assigned
        /// collection.
        /// </remarks>
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

        /// <summary>
        /// Gets the logical children of the control.
        /// </summary>
        IPerspexReadOnlyList<ILogical> ILogical.LogicalChildren
        {
            get { return this.children; }
        }

        /// <inheritdoc/>
        ILogical IItemsPanel.ChildLogicalParent
        {
            get
            {
                return this.childLogicalParent;
            }

            set
            {
                this.childLogicalParent = value;
                this.SetLogicalParent(this.Children);
            }
        }

        /// <inheritdoc/>
        protected virtual void OnChildrenAdded(IEnumerable<Control> child)
        {
        }

        /// <inheritdoc/>
        protected virtual void OnChildrenRemoved(IEnumerable<Control> child)
        {
        }

        /// <summary>
        /// Clears <see cref="IControl.Parent"/> for the specified controls.
        /// </summary>
        /// <param name="controls">The controls.</param>
        private void ClearLogicalParent(IEnumerable<Control> controls)
        {
            foreach (var control in controls)
            {
                control.Parent = null;
            }
        }

        /// <summary>
        /// Sets <see cref="IControl.Parent"/> for the specified controls.
        /// </summary>
        /// <param name="controls">The controls.</param>
        private void SetLogicalParent(IEnumerable<Control> controls)
        {
            foreach (var control in controls)
            {
                control.Parent = this.childLogicalParent as Control;
            }
        }

        /// <summary>
        /// Called when the <see cref="Children"/> collection changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            List<Control> controls;

            // TODO: Handle Replace.
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    controls = e.NewItems.OfType<Control>().ToList();
                    this.SetLogicalParent(controls);
                    this.AddVisualChildren(e.NewItems.OfType<Visual>());
                    this.OnChildrenAdded(controls);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    controls = e.OldItems.OfType<Control>().ToList();
                    this.ClearLogicalParent(e.OldItems.OfType<Control>());
                    this.RemoveVisualChildren(e.OldItems.OfType<Visual>());
                    this.OnChildrenRemoved(controls);
                    break;

                case NotifyCollectionChangedAction.Reset:
                    controls = e.OldItems.OfType<Control>().ToList();
                    this.ClearLogicalParent(controls);
                    this.ClearVisualChildren();
                    this.AddVisualChildren(this.children);
                    this.OnChildrenAdded(controls);
                    break;
            }

            this.InvalidateMeasure();
        }
    }
}
