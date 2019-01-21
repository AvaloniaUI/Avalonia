using System;

namespace Avalonia.OpenGL
{
    public class GlEntryPointAttribute : Attribute
    {
        public string EntryPoint { get; }
        public bool Optional { get; }

        public GlEntryPointAttribute(string entryPoint, bool optional = false)
        {
            EntryPoint = entryPoint;
            Optional = optional;
        }
    }
}
