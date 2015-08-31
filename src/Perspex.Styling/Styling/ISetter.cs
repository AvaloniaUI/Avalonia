namespace Perspex.Styling
{
    using System;

    /// <summary>
    /// Represents a setter for a <see cref="Style"/>.
    /// </summary>
    public interface ISetter
    {
        /// <summary>
        /// Applies the setter to the control.
        /// </summary>
        /// <param name="style">The style that is being applied.</param>
        /// <param name="control">The control.</param>
        /// <param name="activator">An optional activator.</param>
        void Apply(IStyle style, IStyleable control, IObservable<bool> activator);
    }
}