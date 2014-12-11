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
    using Perspex.Controls.Primitives;
    using Perspex.Layout;

    public class ContentPresenter : Control, IVisual
    {
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ContentPresenter>();

        private bool createdChild;

        public ContentPresenter()
        {
            this.GetObservable(ContentProperty).Skip(1).Subscribe(this.ContentChanged);
        }

        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
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
            Control child = ((IVisual)this).VisualChildren.SingleOrDefault() as Control;

            if (child != null)
            {
                child.Measure(availableSize);
                return child.DesiredSize.Value;
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
            object content = this.Content;

            this.ClearVisualChildren();

            if (content != null)
            {
                Control result;

                if (content is Control)
                {
                    result = (Control)content;
                }
                else
                {
                    DataTemplate dataTemplate = this.FindDataTemplate(content);

                    if (dataTemplate != null)
                    {
                        result = dataTemplate.Build(content);
                    }
                    else
                    {
                        result = new TextBlock
                        {
                            Text = content.ToString(),
                        };
                    }
                }

                var foo = this.TemplatedParent as TemplatedControl;

                if (foo != null)
                {
                    foo = foo.TemplatedParent as TemplatedControl;
                }

                result.TemplatedParent = foo;
                this.AddVisualChild(result);
            }

            this.createdChild = true;
        }
    }
}
