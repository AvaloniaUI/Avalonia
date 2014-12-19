// -----------------------------------------------------------------------
// <copyright file="ContentControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Collections;
    using Perspex.Controls.Primitives;
    using Perspex.Layout;

    public class ContentControl : TemplatedControl, ILogical
    {
        public static readonly PerspexProperty<object> ContentProperty =
            PerspexProperty.Register<ContentControl, object>("Content");

        public static readonly PerspexProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            PerspexProperty.Register<ContentControl, HorizontalAlignment>("HorizontalContentAlignment");

        public static readonly PerspexProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            PerspexProperty.Register<ContentControl, VerticalAlignment>("VerticalContentAlignment");

        private SingleItemPerspexList<ILogical> logicalChild = new SingleItemPerspexList<ILogical>();

        public ContentControl()
        {
            this.GetObservableWithHistory(ContentProperty).Subscribe(x =>
            {
                var control1 = x.Item1 as Control;
                var control2 = x.Item2 as Control;

                if (control1 != null)
                {
                    control1.Parent = null;
                }

                if (control2 != null)
                {
                    control2.Parent = this;
                }

                this.logicalChild.SingleItem = control2;
            });
        }

        public object Content
        {
            get { return this.GetValue(ContentProperty); }
            set { this.SetValue(ContentProperty, value); }
        }

        IReadOnlyPerspexList<ILogical> ILogical.LogicalChildren
        {
            get { return this.logicalChild; }
        }

        public HorizontalAlignment HorizontalContentAlignment
        {
            get { return this.GetValue(HorizontalContentAlignmentProperty); }
            set { this.SetValue(HorizontalContentAlignmentProperty, value); }
        }

        public VerticalAlignment VerticalContentAlignment
        {
            get { return this.GetValue(VerticalContentAlignmentProperty); }
            set { this.SetValue(VerticalContentAlignmentProperty, value); }
        }
    }
}
