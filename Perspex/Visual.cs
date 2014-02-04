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

    public abstract class Visual : PerspexObject
    {
        private string id;

        private Visual visualParent;

        public Rect Bounds
        {
            get;
            protected set;
        }

        public string Id
        {
            get
            {
                return this.id;
            }

            set
            {
                if (this.id != null)
                {
                    throw new InvalidOperationException("ID already set.");
                }

                if (this.visualParent != null)
                {
                    throw new InvalidOperationException("Cannot set ID : control already added to tree.");
                }

                this.id = value;
            }
        }

        public virtual IEnumerable<Visual> VisualChildren
        {
            get { return Enumerable.Empty<Visual>(); }
        }

        public Visual VisualParent
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
                    this.InheritanceParent = value;
                }
            }
        }

        public virtual void Render(IDrawingContext context)
        {
            Contract.Requires<ArgumentNullException>(context != null);
        }
    }
}
