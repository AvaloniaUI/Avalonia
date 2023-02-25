using System;

namespace Avalonia.Metadata
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class TrimSurroundingWhitespaceAttribute : Attribute
    {

    }
}
