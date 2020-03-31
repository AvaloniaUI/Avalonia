using Avalonia.Collections;

namespace Avalonia.Animation
{
    /// <summary>
    /// A collection of <see cref="ITransition"/> definitions.
    /// </summary>
    public class Transitions : AvaloniaList<ITransition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Transitions"/> class.
        /// </summary>
        public Transitions()
        {
            ResetBehavior = ResetBehavior.Remove;
        }
    }
}
