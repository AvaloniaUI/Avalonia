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

    public class ContentPresenter : Control, IVisual
    {
        public static readonly PerspexProperty<object> ContentProperty =
            ContentControl.ContentProperty.AddOwner<Control>();

        public static readonly PerspexProperty<Func<object, Visual>> DataTemplateProperty =
            PerspexProperty.Register<ContentPresenter, Func<object, Visual>>("DataTemplate");

        private IVisual visualChild;

        public ContentPresenter()
        {
            this.GetObservableWithHistory(ContentProperty).Subscribe(x =>
            {
                if (x.Item1 is Control)
                {
                    ((IVisual)x.Item1).VisualParent = null;
                    ((ILogical)x.Item1).LogicalParent = null;
                }

                if (x.Item2 is Control)
                {
                    ((IVisual)x.Item2).VisualParent = this;
                    ((ILogical)x.Item2).LogicalParent = this;
                }
            });
        }

        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        IEnumerable<IVisual> IVisual.ExistingVisualChildren
        {
            get { return Enumerable.Repeat(this.visualChild, this.visualChild != null ? 1 : 0); }
        }

        IEnumerable<IVisual> IVisual.VisualChildren
        {
            get
            {
                object content = this.Content;

                if (this.visualChild == null && content != null)
                {
                    if (content is Visual)
                    {
                        this.visualChild = (Visual)content;
                    }
                    else
                    {
                        DataTemplate dataTemplate = this.FindDataTemplate(content);

                        if (dataTemplate != null)
                        {
                            this.visualChild = dataTemplate.Build(content);
                        }
                        else
                        {
                            this.visualChild = new TextBlock
                            {
                                Text = content.ToString(),
                            };
                        }
                    }

                    if (this.visualChild != null)
                    {
                        ((IVisual)this.visualChild).VisualParent = this;
                    }
                }

                return Enumerable.Repeat(this.visualChild, this.visualChild != null ? 1 : 0);
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
    }
}
