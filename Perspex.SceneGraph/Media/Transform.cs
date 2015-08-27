// -----------------------------------------------------------------------
// <copyright file="Transform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using Perspex.Animation;

    /// <summary>
    /// Represents a transform on an <see cref="IVisual"/>.
    /// </summary>
    public abstract class Transform : Animatable
    {
        /// <summary>
        /// Raised when the transform changes.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Gets the tranform's <see cref="Matrix"/>.
        /// </summary>
        public abstract Matrix Value { get; }

        /// <summary>
        /// Raises the <see cref="Changed"/> event.
        /// </summary>
        protected void RaiseChanged()
        {
            if (this.Changed != null)
            {
                this.Changed(this, EventArgs.Empty);
            }
        }
    }
}
