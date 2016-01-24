// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;

namespace Perspex.Controls.Presenters
{
    /// <summary>
    /// Presents a single item of data inside a <see cref="TemplatedControl"/> template.
    /// </summary>
    public class ContentPresenter : Control, IContentPresenter
    {
        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly PerspexProperty<IControl> ChildProperty =
            PerspexProperty.RegisterDirect<ContentPresenter, IControl>(
                nameof(Child),
                o => o.Child);

        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ContentPresenter>();

        private IControl _child;
        private bool _createdChild;

        /// <summary>
        /// Initializes static members of the <see cref="ContentPresenter"/> class.
        /// </summary>
        static ContentPresenter()
        {
            ContentProperty.Changed.AddClassHandler<ContentPresenter>(x => x.ContentChanged);
            TemplatedParentProperty.Changed.AddClassHandler<ContentPresenter>(x => x.TemplatedParentChanged);
        }

        /// <summary>
        /// Gets the control displayed by the presenter.
        /// </summary>
        public IControl Child
        {
            get { return _child; }
            private set { SetAndRaise(ChildProperty, ref _child, value); }
        }

        /// <summary>
        /// Gets or sets the content to be displayed by the presenter.
        /// </summary>
        public object Content
        {
            get { return GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        /// <inheritdoc/>
        public override sealed void ApplyTemplate()
        {
            if (!_createdChild && ((ILogical)this).IsAttachedToLogicalTree)
            {
                UpdateChild();
            }
        }

        /// <summary>
        /// Updates the <see cref="Child"/> control based on the control's <see cref="Content"/>.
        /// </summary>
        /// <remarks>
        /// Usually the <see cref="Child"/> control is created automatically when 
        /// <see cref="ApplyTemplate"/> is called; however for this to happen, the control needs to
        /// be attached to a logical tree (if the control is not attached to the logical tree, it
        /// is reasonable to expect that the DataTemplates needed for the child are not yet 
        /// available). This method forces the <see cref="Child"/> control's creation at any point, 
        /// and is particularly useful in unit tests.
        /// </remarks>
        public void UpdateChild()
        {
            var old = Child;
            var content = Content;
            var result = this.MaterializeDataTemplate(content);

            if (old != null)
            {
                VisualChildren.Remove(old);
            }

            if (result != null)
            {
                if (!(content is IControl))
                {
                    result.DataContext = content;
                }

                Child = result;

                if (result.Parent == null)
                {
                    ((ISetLogicalParent)result).SetParent((ILogical)this.TemplatedParent ?? this);
                }

                VisualChildren.Add(result);
            }
            else
            {
                Child = null;
            }

            _createdChild = true;
        }

        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            _createdChild = false;
            InvalidateMeasure();
        }

        /// <inheritdoc/>
        protected override Size MeasureCore(Size availableSize)
        {
            return base.MeasureCore(availableSize);
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var child = Child;

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
            _createdChild = false;
            InvalidateMeasure();
        }

        private void TemplatedParentChanged(PerspexPropertyChangedEventArgs e)
        {
            (e.NewValue as IContentPresenterHost)?.RegisterContentPresenter(this);
        }
    }
}
