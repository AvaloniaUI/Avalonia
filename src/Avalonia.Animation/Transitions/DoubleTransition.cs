// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Metadata;
using System;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transitions object that handles properties with <see cref="double"/> types.
    /// </summary>  
    public class DoubleTransition : Transition<double>
    {
        /// <inheritdocs/>
        public DoubleTransition() : base()
        {

        }

        /// <inheritdocs/>
        public override AvaloniaProperty Property { get; set; }

        /// <inheritdocs/>
        public override void Apply(Animatable control)
        {
            //throw new NotImplementedException();



        }
    }
}
