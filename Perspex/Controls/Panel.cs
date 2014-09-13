// -----------------------------------------------------------------------
// <copyright file="Panel.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Base class for controls that can contain multiple children.
    /// </summary>
    public class Panel : Control, IVisual
    {
        private Controls children;

        private LogicalChildren<Control> logicalChildren;

        public Controls Children
        {
            get
            {
                if (this.children == null)
                {
                    this.children = new Controls();
                    this.logicalChildren = new LogicalChildren<Control>(this, this.children);
                }

                return this.children;
            }
            
            set
            {
                this.children = value;

                if (this.logicalChildren != null)
                {
                    this.logicalChildren.Change(this.children);
                }
                else
                {
                    this.logicalChildren = new LogicalChildren<Control>(this, this.children);
                }
            }
        }

        IEnumerable<IVisual> IVisual.VisualChildren
        {
            get { return this.children ?? Enumerable.Empty<IVisual>(); }
        }
    }
}
