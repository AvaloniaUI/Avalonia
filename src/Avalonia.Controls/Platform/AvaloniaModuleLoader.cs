using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia
{
    class AvaloniaModuleLoader<TAppBuilder>
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
    {
        private Dictionary<string, ModuleGroupInformation> moduleGroups;

        private class ModuleInformation
        {
            public ModuleInformation(ExportAvaloniaModuleAttribute attribute, ConstructorInfo initializer)
            {
                Attribute = attribute;
                Initializer = initializer;
            }
            public ExportAvaloniaModuleAttribute Attribute { get; set; }
            public ConstructorInfo Initializer { get; set; }
        }

        private class ModuleGroupInformation
        {
            public ModuleGroupInformation(Action initializer)
            {
                this.initializer = initializer;
            }
            private Action initializer;
            private bool initialized;
            private bool initializing;

            public void TryInitialize()
            {
                if (initializing)
                {
                    throw new InvalidOperationException("Cyclic dependency in Avalonia Modules found.");
                }
                initializing = true;
                if (!initialized)
                {
                    initializer();
                    initialized = true;
                }
                initializing = false;
            }
        }

        public AvaloniaModuleLoader(TAppBuilder builder)
        {
            var runtimePlatform = AvaloniaLocator.Current.GetService<IRuntimePlatform>();
            moduleGroups = (from assembly in runtimePlatform.GetLoadedAssemblies()
                            from attribute in assembly.GetCustomAttributes<ExportAvaloniaModuleAttribute>()
                            where attribute.ForWindowingSubsystem == ""
                             || attribute.ForWindowingSubsystem == builder.WindowingSubsystemName
                            where attribute.ForRenderingSubsystem == ""
                             || attribute.ForRenderingSubsystem == builder.RenderingSubsystemName
                            where attribute.ForOperatingSystem == OperatingSystemType.Unknown
                             || attribute.ForOperatingSystem == runtimePlatform.GetRuntimeInfo().OperatingSystem
                            let initializers = from constructor in attribute.ModuleType.GetTypeInfo().DeclaredConstructors
                                               where constructor.GetParameters().Length == 0 && !constructor.IsStatic
                                               select constructor
                            select new ModuleInformation(attribute, initializers.First()) into module
                            group module by module.Attribute.Name into moduleGroup
                            let orderedModuleOptions = (from module in moduleGroup
                                                        orderby module.Attribute.ForOperatingSystem descending
                                                        orderby module.Attribute.ForWindowingSubsystem descending
                                                        orderby module.Attribute.ForRenderingSubsystem descending
                                                        select module)
                            select new
                            {
                                Name = moduleGroup.Key,
                                Info = new ModuleGroupInformation(() => InitializeGroup(moduleGroup.Key, orderedModuleOptions))
                            }).ToDictionary(info => info.Name, info => info.Info);
        }

        public void LoadModules()
        {
            foreach (var groupedModule in moduleGroups)
            {
                groupedModule.Value.TryInitialize();
            }
        }

        private void InitializeGroup(string name, IEnumerable<ModuleInformation> group)
        {
            Logger.Information(LogArea.Module, this, "Loading module group {0}", name);
            var exceptions = new List<Exception>();
            foreach (var module in group)
            {
                Logger.Debug(LogArea.Module, this, "Loading dependencies module with type {0}", module.Attribute.ModuleType);
                foreach (var dependency in module.Attribute.DependsOnModules)
                {
                    moduleGroups[dependency].TryInitialize();
                }
                try
                {
                    Logger.Debug(LogArea.Module, this, "Loading dependencies module {0} with type {1}", name, module.Attribute.ModuleType);
                    module.Initializer.Invoke(null);
                    return;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }

    public static class AvaloniaModuleLoadExtensions
    {
        public static TAppBuilder UseAvaloniaModules<TAppBuilder>(this TAppBuilder builder)
            where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
        {
            return builder.AfterSetup(appBuilder => new AvaloniaModuleLoader<TAppBuilder>(appBuilder).LoadModules());
        }
    }
}
