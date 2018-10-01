using System;

namespace Avalonia.OpenGL
{
    class EntryPointAttribute : Attribute
    {
        public string EntryPoint { get; }
        public bool Optional { get; }

        public EntryPointAttribute(string entryPoint, bool optional = false)
        {
            EntryPoint = entryPoint;
            Optional = optional;
        }
    }
}
