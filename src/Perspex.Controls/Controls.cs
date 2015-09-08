// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Perspex.Collections;

namespace Perspex.Controls
{
    /// <summary>
    /// A collection of <see cref="Control"/>s.
    /// </summary>
    public class Controls : PerspexList<IControl>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Controls"/> class.
        /// </summary>
        public Controls()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Controls"/> class.
        /// </summary>
        /// <param name="items">The initial items in the collection.</param>
        public Controls(IEnumerable<IControl> items)
            : base(items)
        {
        }
    }
}
