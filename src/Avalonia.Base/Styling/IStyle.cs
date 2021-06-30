using System.Collections.Generic;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Represents a style or a collection of styles.
    /// </summary>
    /// <remarks>
    /// Represents either a <see cref="Style"/> or an <see cref="IEnumerable{Style}"/>; all other
    /// implementations of this interface will be ignored. I wish C# had discriminated unions.
    /// </remarks>
    public interface IStyle
    {
    }
}
