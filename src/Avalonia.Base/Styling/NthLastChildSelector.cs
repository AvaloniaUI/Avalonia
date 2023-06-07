#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// The :nth-child() pseudo-class matches elements based on their position among a group of siblings, counting from the end.
    /// </summary>
    /// <remarks>
    /// Element indices are 1-based.
    /// </remarks>
    internal class NthLastChildSelector : NthChildSelector
    {
        /// <summary>
        /// Creates an instance of <see cref="NthLastChildSelector"/>
        /// </summary>
        /// <param name="previous">Previous selector.</param>
        /// <param name="step">Position step.</param>
        /// <param name="offset">Initial index offset, counting from the end.</param>
        public NthLastChildSelector(Selector? previous, int step, int offset) : base(previous, step, offset, true)
        {
        }
    }
}
