// -----------------------------------------------------------------------
// <copyright file="ContentPresenter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Presenters
{
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Collections;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;

    /// <summary>
    /// Presents a single item of data inside a <see cref="TemplatedControl"/> template.
    /// </summary>
    public class ContentPresenter : Control, IPresenter
    {
        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ContentPresenter>();

        private bool createdChild;

        private ILogical logicalParent;

        /// <summary>
        /// Initializes static members of the <see cref="ContentPresenter"/> class.
        /// </summary>
        static ContentPresenter()
        {
            ContentProperty.Changed.AddClassHandler<ContentPresenter>(x => x.ContentChanged);
        }

        /// <summary>
        /// Gets the control displayed by the presenter.
        /// </summary>
        public IControl Child
        {
            get { return (Control)this.LogicalChildren.SingleOrDefault(); }
        }

        /// <summary>
        /// Gets or sets the content to be displayed by the presenter.
        /// </summary>
        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        /// <inheritdoc/>
        public override sealed void ApplyTemplate()
        {
            if (!this.createdChild)
            {
                this.CreateChild();
            }
        }

        /// <inheritdoc/>
        void IReparentingControl.ReparentLogicalChildren(ILogical logicalParent, IPerspexList<ILogical> children)
        {
            if (this.Child != null)
            {
                ((ISetLogicalParent)this.Child).SetParent(null);
                ((ISetLogicalParent)this.Child).SetParent(logicalParent);
                children.Add(this.Child);
            }

            this.logicalParent = logicalParent;
            this.RedirectLogicalChildren(children);
        }

        /// <inheritdoc/>
        protected override Size MeasureCore(Size availableSize)
        {
            return base.MeasureCore(availableSize);
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var child = this.Child;

            if (child != null)
            {
                child.Measure(availableSize);
                return child.DesiredSize;
            }

            return new Size();
        }

        /// <summary>
        /// Called when the <see cref="Content"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ContentChanged(PerspexPropertyChangedEventArgs e)
        {
            this.createdChild = false;
            this.InvalidateMeasure();
        }

        /// <summary>
        /// Creates the <see cref="Child"/> control from the <see cref="Content"/>.
        /// </summary>
        private void CreateChild()
        {
            IControl result = null;
            object content = this.Content;

            this.LogicalChildren.Clear();
            this.ClearVisualChildren();

            if (content != null)
            {
                result = this.MaterializeDataTemplate(content);

                if (result.Parent == null)
                {
                    ((ISetLogicalParent)result).SetParent(this.logicalParent ?? this);
                }

                var templatedParent = this.TemplatedParent as TemplatedControl;

                if (templatedParent != null)
                {
                    templatedParent = templatedParent.TemplatedParent as TemplatedControl;
                }

                ((Control)result).TemplatedParent = templatedParent;
                this.AddVisualChild(result);
                this.LogicalChildren.Add(result);
            }

            this.createdChild = true;
        }
    }
}
