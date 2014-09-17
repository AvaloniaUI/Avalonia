// -----------------------------------------------------------------------
// <copyright file="ContentPresenter.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Styling;

    public class ContentPresenter : Control, IVisual
    {
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<Control>();

        public static readonly PerspexProperty<Func<object, Visual>> DataTemplateProperty =
            PerspexProperty.Register<ContentPresenter, Func<object, Visual>>("DataTemplate");

        public ContentPresenter()
        {
            this.GetObservableWithHistory(ContentProperty).Subscribe(this.ContentChanged);
        }

        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        protected override IEnumerable<Visual> CreateVisualChildren()
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
                return Enumerable.Repeat(result, 1);
            }
            else
            {
                return Enumerable.Empty<Visual>();
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Control child = ((IVisual)this).VisualChildren.SingleOrDefault() as Control;

            if (child != null)
            {
                double left;
                double top;
                double width;
                double height;

                switch (child.HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        left = 0;
                        width = child.DesiredSize.Value.Width;
                        break;
                    case HorizontalAlignment.Center:
                        left = (finalSize.Width / 2) - (child.DesiredSize.Value.Width / 2);
                        width = child.DesiredSize.Value.Width;
                        break;
                    case HorizontalAlignment.Right:
                        left = finalSize.Width - child.DesiredSize.Value.Width;
                        width = child.DesiredSize.Value.Width;
                        break;
                    default:
                        left = 0;
                        width = finalSize.Width;
                        break;
                }

                switch (child.VerticalAlignment)
                {
                    case VerticalAlignment.Top:
                        top = 0;
                        height = child.DesiredSize.Value.Height;
                        break;
                    case VerticalAlignment.Center:
                        top = (finalSize.Height / 2) - (child.DesiredSize.Value.Height / 2);
                        height = child.DesiredSize.Value.Height;
                        break;
                    case VerticalAlignment.Bottom:
                        top = finalSize.Height - child.DesiredSize.Value.Height;
                        height = child.DesiredSize.Value.Height;
                        break;
                    default:
                        top = 0;
                        height = finalSize.Height;
                        break;
                }

                child.Arrange(new Rect(left, top, width, height));
            }

            return finalSize;
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

        private void ContentChanged(Tuple<object, object> content)
        {
            var children = this.CreateVisualChildren();
            this.ClearVisualChildren();
            this.AddVisualChildren(children);
            this.InvalidateMeasure();
        }
    }
}
