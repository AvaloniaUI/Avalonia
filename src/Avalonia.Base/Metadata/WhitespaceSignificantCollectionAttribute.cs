using System;

namespace Avalonia.Metadata
{
    /// <summary>
    /// Indicates that a collection type should be processed as being whitespace significant by a XAML processor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class WhitespaceSignificantCollectionAttribute : Attribute
    {
    }
}
