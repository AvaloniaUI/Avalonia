// -----------------------------------------------------------------------
// <copyright file="Visual.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using Perspex.Media;

    public abstract class Visual : PerspexObject, IVisual, ILogical
    {
        private ILogical logicalParent;

        private IVisual visualParent;

        public Rect Bounds
        {
            get;
            protected set;
        }

        ILogical ILogical.LogicalParent
        {
            get { return this.logicalParent; }
            set { this.logicalParent = value; }
        }

        IEnumerable<ILogical> ILogical.LogicalChildren
        {
            get { return new ILogical[0]; }
        }

        IEnumerable<IVisual> IVisual.VisualChildren
        {
            get { return Enumerable.Empty<Visual>(); }
        }

        IVisual IVisual.VisualParent
        {
            get 
            { 
                return this.visualParent;
            }

            set
            {
                if (this.visualParent != value)
                {
                    this.visualParent = value;
                    this.InheritanceParent = (PerspexObject)value;
                }
            }
        }

        public virtual void Render(IDrawingContext context)
        {
            Contract.Requires<ArgumentNullException>(context != null);
        }
    }
}
