using System;

namespace Avalonia.Platform
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class ExportAvaloniaModuleAttribute : Attribute
    {
        public ExportAvaloniaModuleAttribute(string name, Type moduleType)
        {
            Name = name;
            ModuleType = moduleType;
        }

        public string Name { get; private set; }
        public Type ModuleType { get; private set; }

        public string RequiredWindowingSubsystem { get; set; } = "";
        public string RequiredRenderingSubsystem { get; set; } = "";
    }
}
