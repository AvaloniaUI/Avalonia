using System;

namespace Avalonia.OpenGL
{
    class EntryPointAttribute : Attribute
    {
        public string EntryPoint { get; }

        public EntryPointAttribute(string entryPoint)
        {
            EntryPoint = entryPoint;
        }
    }
}
