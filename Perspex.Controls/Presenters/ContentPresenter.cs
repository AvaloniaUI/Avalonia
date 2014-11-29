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
    using Perspex.Layout;

    public class ContentPresenter : Control, IVisual
    {
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<Control>();

        public ContentPresenter()
        {
            this.GetObservable(ContentProperty).Skip(1).Subscribe(this.ContentChanged);
        }

        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        protected override void CreateVisualChildren()
        {
            object content = this.Content;

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

                result.TemplatedParent = null;
                this.AddVisualChild(result);
            }
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
            this.ClearVisualChildren();
            this.CreateVisualChildren();
            this.InvalidateMeasure();
        }
    }
}
