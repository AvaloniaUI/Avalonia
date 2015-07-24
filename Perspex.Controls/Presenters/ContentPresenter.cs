// -----------------------------------------------------------------------
// <copyright file="ContentPresenter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Presenters
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Perspex.Collections;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Media;

    public class ContentPresenter : Control, IVisual, ILogical, IPresenter
    {
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ContentPresenter>();

        private bool createdChild;

        private PerspexSingleItemList<ILogical> logicalChild = new PerspexSingleItemList<ILogical>();

        public ContentPresenter()
        {
            this.GetObservable(ContentProperty).Skip(1).Subscribe(this.ContentChanged);
        }

        public Control Child
        {
            get { return (Control)this.logicalChild.SingleItem; }
        }

        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        IPerspexReadOnlyList<ILogical> ILogical.LogicalChildren
        {
            get { return this.logicalChild; }
        }

        public override sealed void ApplyTemplate()
        {
            if (!this.createdChild)
            {
                this.CreateChild();
            }
        }

        protected override Size MeasureCore(Size availableSize)
        {
            return base.MeasureCore(availableSize);
        }

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

        private void ContentChanged(object content)
        {
            this.createdChild = false;
            this.InvalidateMeasure();
        }

        private void CreateChild()
        {
            Control result = null;
            object content = this.Content;

            this.ClearVisualChildren();

            if (content != null)
            {
                result = this.MaterializeDataTemplate(content);

                if (result.Parent == null)
                {
                    ((ISetLogicalParent)result).SetParent(this.TemplatedParent as Control);
                }

                var templatedParent = this.TemplatedParent as TemplatedControl;

                if (templatedParent != null)
                {
                    templatedParent = templatedParent.TemplatedParent as TemplatedControl;
                }

                result.TemplatedParent = templatedParent;
                this.AddVisualChild(result);
            }

            this.logicalChild.SingleItem = result;
            this.createdChild = true;
        }
    }
}
