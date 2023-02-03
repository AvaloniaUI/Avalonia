using System;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines an "Avalonia Module", a 3rd party extension to Avalonia that can be automatically initialized by an AppBuilder instance.
    /// </summary>
    /// <remarks>
    /// Avalonia Modules can either be platform independent (ex default control styles provider) or dependent on a
    /// specific windowing or rendering subsystem being used (ex native rendering speedup, subsystem-specific interop backends).
    /// In the case of a subsystem-specific module, you can specify multiple module implementations, and also a fallback
    /// platform-independent module if you so choose. Additionally, these different implementations can be in different assemblies.
    /// They just need to all share the same module name.
    /// 
    /// For example, if I had a module Foo that has a special back-end for Skia and a less performant/less user friendly back-end for
    /// any other rendering subsystem, I would do the following:
    /// <code>
    /// // In assembly FooModuleSkia.dll
    /// [assembly:ExportAvaloniaModule("Foo", typeof(FooModuleSkia), ForRenderingSubsystem="Skia")]
    /// 
    /// class FooModuleSkia
    /// {
    ///     public FooModuleSkia()
    ///     {
    ///         InitializeModule();
    ///     }
    /// }
    /// 
    /// // In assembly FooModuleFallback.dll
    /// [assembly:ExportAvaloniaModule("Foo", typeof(FooModuleFallback))]
    /// 
    /// class FooModuleFallback
    /// {
    ///     public FooModuleFallback()
    ///     {
    ///         InitializeModule();
    ///     }
    /// }
    /// 
    /// </code>
    /// The fallback module will only be initialized if the Skia-specific module is not applicable.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ExportAvaloniaModuleAttribute : Attribute
    {
        public ExportAvaloniaModuleAttribute(string name, Type moduleType)
        {
            Name = name;
            ModuleType = moduleType;
        }

        public string Name { get; private set; }
        public Type ModuleType { get; private set; }

        public string ForWindowingSubsystem { get; set; } = "";
        public string ForRenderingSubsystem { get; set; } = "";
    }
}
