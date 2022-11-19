using System;
using Avalonia.Metadata;

namespace Avalonia.Styling
{
    /// <summary>
    /// Represents a setter for a <see cref="Style"/>.
    /// </summary>
    [NotClientImplementable]
    public interface ISetter
    {
        /// <summary>
        /// Instances a setter on a control.
        /// </summary>
        /// <param name="styleInstance">The style which contains the setter.</param>
        /// <param name="target">The control.</param>
        /// <returns>An <see cref="ISetterInstance"/>.</returns>
        /// <remarks>
        /// This method should return an <see cref="ISetterInstance"/> which can be used to apply
        /// the setter to the specified control.
        /// </remarks>
        ISetterInstance Instance(IStyleInstance styleInstance, IStyleable target);
    }
}
