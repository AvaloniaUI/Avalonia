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
    using Perspex.Media;

    public class ContentPresenter : Control
    {
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<Control>();

        public static readonly PerspexProperty<Func<object, Visual>> DataTemplateProperty =
            PerspexProperty.Register<ContentPresenter, Func<object, Visual>>("DataTemplate");

        private Visual visualChild;

        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        public Func<object, Visual> DataTemplate
        {
            get { return this.GetValue(DataTemplateProperty); }
            set { this.SetValue(DataTemplateProperty, value); }
        }

        public override IEnumerable<IVisual> VisualChildren
        {
            get
            {
                object content = this.Content;
                var dataTemplate = this.DataTemplate;

                if (this.visualChild == null && content != null)
                {
                    if (content is Visual)
                    {
                        this.visualChild = (Visual)content;
                    }
                    else if (dataTemplate != null)
                    {
                        this.visualChild = dataTemplate(this);
                    }
                    else
                    {
                        this.visualChild = new TextBlock
                        {
                            Text = content.ToString(),
                        };
                    }

                    if (this.visualChild != null)
                    {
                        this.visualChild.VisualParent = this;
                    }
                }

                return Enumerable.Repeat(this.visualChild, this.visualChild != null ? 1 : 0);
            }
        }

        protected override Size ArrangeContent(Size finalSize)
        {
            Control child = this.VisualChildren.SingleOrDefault() as Control;

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

        protected override Size MeasureContent(Size availableSize)
        {
            Control child = this.VisualChildren.SingleOrDefault() as Control;

            if (child != null)
            {
                child.Measure(availableSize);
                return child.DesiredSize.Value;
            }
            else
            {
                return new Size();
            }
        }
    }
}
