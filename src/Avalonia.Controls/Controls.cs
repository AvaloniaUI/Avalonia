using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// A collection of <see cref="Control"/>s.
    /// </summary>
    public class Controls : AvaloniaList<IControl>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Controls"/> class.
        /// </summary>
        public Controls()
        {
            ResetBehavior = ResetBehavior.Remove;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Controls"/> class.
        /// </summary>
        /// <param name="items">The initial items in the collection.</param>
        public Controls(IEnumerable<IControl> items)
            : base(items)
        {
            ResetBehavior = ResetBehavior.Remove;
        }
    }
}
