// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Collections;

namespace Perspex.Animation
{
    /// <summary>
    /// A collection of <see cref="PropertyTransition"/> definitions.
    /// </summary>
    public class PropertyTransitions : PerspexList<PropertyTransition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyTransitions"/> class.
        /// </summary>
        public PropertyTransitions()
        {
            ResetBehavior = ResetBehavior.Remove;
        }
    }
}
