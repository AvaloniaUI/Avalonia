using System;

namespace Avalonia.Metadata
{
    /// <summary>
    /// This interface is not intended to be implemented outside of the core Avalonia framework as
    /// its API may change without warning.
    /// </summary>
    /// <remarks>
    /// This interface is stable for consumption by a client, but should not be implemented as members
    /// may be added to its API.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Interface)]
    public sealed class NotClientImplementableAttribute : Attribute
    {
    }
}
