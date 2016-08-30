using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;
using System.IO;

namespace Avalonia
{
    public sealed class AppBuilder : AppBuilderBase<AppBuilder>
    {
        public AppBuilder() : base(new StandardRuntimePlatform(), () => StandardRuntimePlatformServices.Register())
        {
        }

        public AppBuilder(Application app) : this()
        {
            Instance = app;
        }

        public AppBuilder UsePlatformDetect()
        {
            var os = RuntimePlatform.GetRuntimeInfo().OperatingSystem;

            LoadAssembliesInDirectory();

            var windowingSubsystemAttribute = (from assembly in RuntimePlatform.GetLoadedAssemblies()
                                               from attribute in assembly.GetCustomAttributes<ExportWindowingSubsystemAttribute>()
                                               where attribute.RequiredOS == os
                                               orderby attribute.Priority ascending
                                               select attribute).First();

            var renderingSubsystemAttribute = (from assembly in RuntimePlatform.GetLoadedAssemblies()
                                               from attribute in assembly.GetCustomAttributes<ExportRenderingSubsystemAttribute>()
                                               where attribute.RequiredOS == os
                                               where attribute.RequiresWindowingSubsystem == null
                                                || attribute.RequiresWindowingSubsystem == windowingSubsystemAttribute.Name
                                               orderby attribute.Priority ascending
                                               select attribute).First();

            UseWindowingSubsystem(() => windowingSubsystemAttribute.InitializationType
                .GetRuntimeMethod(windowingSubsystemAttribute.InitializationMethod, Type.EmptyTypes).Invoke(null, null));
            WindowingSubsystemName = windowingSubsystemAttribute.Name;

            UseRenderingSubsystem(() => renderingSubsystemAttribute.InitializationType
                .GetRuntimeMethod(renderingSubsystemAttribute.InitializationMethod, Type.EmptyTypes).Invoke(null, null));
            RenderingSubsystemName = renderingSubsystemAttribute.Name;
            
            return this;
        }

        private void LoadAssembliesInDirectory()
        {
            foreach (var file in new FileInfo(Assembly.GetEntryAssembly().Location).Directory.EnumerateFiles("*.dll"))
            {
                try
                {
                    Assembly.LoadFile(file.FullName);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
