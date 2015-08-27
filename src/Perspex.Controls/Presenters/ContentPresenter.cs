// -----------------------------------------------------------------------
// <copyright file="ContentPresenter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Presenters
{
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;

    /// <summary>
    /// Presents a single item of data inside a <see cref="TemplatedControl"/> template.
    /// </summary>
    public class ContentPresenter : Control, IContentPresenter
    {
        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly PerspexProperty<IControl> ChildProperty =
            PerspexProperty.Register<ContentPresenter, IControl>("Child");

        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ContentPresenter>();

        private bool createdChild;

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
            get { return this.GetValue(ChildProperty); }
            private set { this.SetValue(ChildProperty, value); }
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
            var old = this.Child;
            var content = this.Content;
            var result = content != null ? this.MaterializeDataTemplate(content) : null;
            var logicalHost = this.FindReparentingHost();
            var logicalChildren = logicalHost?.LogicalChildren ?? this.LogicalChildren;

            logicalChildren.Remove(old);
            this.ClearVisualChildren();

            this.Child = result;

            if (result != null)
            {
                this.AddVisualChild(result);

                if (result.Parent == null)
                {
                    ((ISetLogicalParent)result).SetParent((ILogical)logicalHost ?? this);
                }

                logicalChildren.Remove(old);
                logicalChildren.Add(result);
            }

            this.createdChild = true;
        }
    }
}
