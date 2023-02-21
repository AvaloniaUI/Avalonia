using System;
using System.Collections.Generic;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// A collection of <see cref="Control"/>s.
    /// </summary>
    public class Controls : AvaloniaList<Control>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Controls"/> class.
        /// </summary>
        public Controls()
        {
            Configure();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Controls"/> class.
        /// </summary>
        /// <param name="items">The initial items in the collection.</param>
        public Controls(IEnumerable<Control> items)
        {
            Configure();
            AddRange(items); // virtual member call in ctor, ok for our current implementation
        }

        private void Configure()
        {
            ResetBehavior = ResetBehavior.Remove;
            Validate = item =>
            {
                if (item is null)
                {
                    throw new ArgumentNullException(nameof(item),
                        $"A null control cannot be added to a {nameof(Controls)} collection.");
                }
            };
        }
    }
}
