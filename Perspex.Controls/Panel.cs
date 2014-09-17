// -----------------------------------------------------------------------
// <copyright file="Panel.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Base class for controls that can contain multiple children.
    /// </summary>
    public class Panel : Control
    {
        private Controls children;

        public Controls Children
        {
            get
            {
                if (this.children == null)
                {
                    this.children = new Controls();
                }

                return this.children;
            }
            
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);

                if (this.children != value)
                {
                    this.children = value;
                    this.ClearVisualChildren();
                    this.AddVisualChildren(value);
                }
            }
        }
    }
}
