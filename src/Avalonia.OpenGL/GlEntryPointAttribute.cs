using System;

namespace Avalonia.OpenGL
{
    [AttributeUsage(AttributeTargets.Property)]
    public class GlEntryPointAttribute : Attribute
    {
        public string[] EntryPoints { get; }
        public bool Optional { get; set; }

        public GlEntryPointAttribute(string entryPoint, bool optional = false)
        {
            EntryPoints = new []{entryPoint};
            Optional = optional;
        }

        public GlEntryPointAttribute(params string[] entryPoints)
        {
            EntryPoints = entryPoints;
        }
    }
}
