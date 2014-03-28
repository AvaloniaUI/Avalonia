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
    public class Panel : Control
    {
        private LogicalChildren<Control> logicalChildren;

        public Panel()
        {
            this.Children = new ObservableCollection<Control>();
            this.logicalChildren = new LogicalChildren<Control>(this, this.Children);
        }

        public ObservableCollection<Control> Children
        {
            get;
            private set;
        }
    }
}
