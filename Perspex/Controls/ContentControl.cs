// -----------------------------------------------------------------------
// <copyright file="ContentControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ContentControl : TemplatedControl, ILogical
    {
        public static readonly PerspexProperty<object> ContentProperty =
            PerspexProperty.Register<ContentControl, object>("Content");

        public ContentControl()
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

        IEnumerable<ILogical> ILogical.LogicalChildren 
        { 
            get
            {
                ILogical logicalChild = this.Content as ILogical;
                return Enumerable.Repeat(logicalChild, logicalChild != null ? 1 : 0);
            }
        }
    }
}
