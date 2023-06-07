using System;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Interface representing a template used to build a control for a piece of data.
    /// </summary>
    public interface IDataTemplate : ITemplate<object?, Control?>
    {
        /// <summary>
        /// Checks to see if this data template matches the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>
        /// True if the data template can build a control for the data, otherwise false.
        /// </returns>
        bool Match(object? data);
    }
}
