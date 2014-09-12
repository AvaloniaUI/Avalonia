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

    public class HeaderedContentControl : ContentControl, ILogical, IHeadered
    {
        public static readonly PerspexProperty<object> HeaderProperty =
            PerspexProperty.Register<ContentControl, object>("Header");

        public object Header
        {
            get { return this.GetValue(HeaderProperty); }
            set { this.SetValue(HeaderProperty, value); }
        }

        IEnumerable<ILogical> ILogical.LogicalChildren
        {
            get
            {
                ILogical logicalContent = this.Content as ILogical;
                ILogical logicalHeader = this.Header as ILogical;

                if (logicalContent != null)
                {
                    yield return logicalContent;
                }

                if (logicalHeader != null)
                {
                    yield return logicalHeader;
                }
            }
        }
    }
}
