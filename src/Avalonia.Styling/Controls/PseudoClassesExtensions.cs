using System;

namespace Avalonia.Controls
{
    public static class PseudolassesExtensions
    {
        /// <summary>
        /// Adds or removes a pseudoclass depending on a boolean value.
        /// </summary>
        /// <param name="classes">The pseudoclasses collection.</param>
        /// <param name="name">The name of the pseudoclass to set. Name need to follow this rules: != null, starts with a ':' and length > 2 (including the ':' char)</param>
        /// <param name="value">True to add the pseudoclass or false to remove.</param>
        public static void Set(this IPseudoClasses classes, string name, bool value)
        {
            _ = classes ?? throw new ArgumentNullException(nameof(classes));

            if (value)
            {
                classes.Add(name);
            }
            else
            {
                classes.Remove(name);
            }
        }

    }
}
