// -----------------------------------------------------------------------
// <copyright file="Transform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using Perspex.Animation;

    public abstract class Transform : Animatable, ITransform
    {
        public event EventHandler Changed;

        public abstract Matrix Value { get; }

        protected void RaiseChanged()
        {
            if (this.Changed != null)
            {
                this.Changed(this, EventArgs.Empty);
            }
        }
    }
}
