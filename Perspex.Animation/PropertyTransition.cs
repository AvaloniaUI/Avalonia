// -----------------------------------------------------------------------
// <copyright file="PropertyTransition.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System;

    public class PropertyTransition
    {
        public PerspexProperty Property { get; set; }

        public TimeSpan Duration { get; set; }

        public IEasing Easing { get; set; }
    }
}
