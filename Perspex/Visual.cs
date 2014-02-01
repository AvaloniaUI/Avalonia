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
        public static readonly ReadOnlyPerspexProperty<Rect> BoundsProperty =
            new ReadOnlyPerspexProperty<Rect>(BoundsPropertyRW);

        public static readonly PerspexProperty<bool> IsMouseOverProperty =
            PerspexProperty.Register<Visual, bool>("IsMouseOver");

        private static readonly PerspexProperty<Rect> BoundsPropertyRW =
            PerspexProperty.Register<Visual, Rect>("Bounds");

        private Visual visualParent;

        public Rect Bounds
        {
            get { return this.GetValue(BoundsPropertyRW); }
            protected set { this.SetValue(BoundsPropertyRW, value); }
        }

        public bool IsMouseOver
        {
            get { return this.GetValue(IsMouseOverProperty); }
            set { this.SetValue(IsMouseOverProperty, value); }
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
