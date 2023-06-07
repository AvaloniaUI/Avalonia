namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// An <see cref="IDataTemplate"/> that supports recycling existing elements.
    /// </summary>
    public interface IRecyclingDataTemplate : IDataTemplate
    {
        /// <summary>
        /// Creates or recycles a control to display the specified data.
        /// </summary>
        /// <param name="data">The data to display.</param>
        /// <param name="existing">An optional control to recycle.</param>
        /// <returns>
        /// The <paramref name="existing"/> control if supplied and applicable to
        /// <paramref name="data"/>, otherwise a new control or null.
        /// </returns>
        /// <remarks>
        /// The caller should ensure that any control passed to <paramref name="existing"/>
        /// originated from the same data template.
        /// </remarks>
        Control? Build(object? data, Control? existing);
    }
}
