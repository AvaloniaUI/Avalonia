// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.VisualTree;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a transform on an <see cref="IVisual"/>.
    /// </summary>
    public abstract class Transform : Animatable
    {
        static Transform()
        {
            Animation.Animation.RegisterAnimator<TransformAnimator>(prop => typeof(Transform).IsAssignableFrom(prop.OwnerType));
        }

        /// <summary>
        /// Raised when the transform changes.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Gets the transform's <see cref="Matrix"/>.
        /// </summary>
        public abstract Matrix Value { get; }

        /// <summary>
        /// Raises the <see cref="Changed"/> event.
        /// </summary>
        protected void RaiseChanged()
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
