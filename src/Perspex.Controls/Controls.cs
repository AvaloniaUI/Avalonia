





namespace Perspex.Controls
{
    using System.Collections.Generic;
    using Perspex.Collections;

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
