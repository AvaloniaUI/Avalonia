using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;

namespace Avalonia
{
    /// <summary>
    /// Initializes platform-specific services for an <see cref="Application"/>.
    /// </summary>
    public sealed class AppBuilder : AppBuilderBase<AppBuilder>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppBuilder"/> class.
        /// </summary>
        public AppBuilder()
            : base(new StandardRuntimePlatform(),
                builder => StandardRuntimePlatformServices.Register(builder.ApplicationType.Assembly))
        {
        }

        bool CheckEnvironment(Type checkerType)
        {
            if (checkerType == null)
                return true;
            try
            {
                return ((IModuleEnvironmentChecker) Activator.CreateInstance(checkerType)).IsCompatible;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Instructs the <see cref="AppBuilder"/> to use the best settings for the platform.
        /// </summary>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder UseSubsystemsFromStartupDirectory()
        {
            var os = RuntimePlatform.GetRuntimeInfo().OperatingSystem;

            LoadAssembliesInDirectory();

            var windowingSubsystemAttribute = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                               from attribute in assembly.GetCustomAttributes<ExportWindowingSubsystemAttribute>()
                                               where attribute.RequiredOS == os && CheckEnvironment(attribute.EnvironmentChecker)
                                               orderby attribute.Priority ascending
                                               select attribute).FirstOrDefault();
            if (windowingSubsystemAttribute == null)
            {
                throw new InvalidOperationException("No windowing subsystem found. Are you missing assembly references?");
            }

            var renderingSubsystemAttribute = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                               from attribute in assembly.GetCustomAttributes<ExportRenderingSubsystemAttribute>()
                                               where attribute.RequiredOS == os && CheckEnvironment(attribute.EnvironmentChecker)
                                               where attribute.RequiresWindowingSubsystem == null
                                                || attribute.RequiresWindowingSubsystem == windowingSubsystemAttribute.Name
                                               orderby attribute.Priority ascending
                                               select attribute).FirstOrDefault();

            if (renderingSubsystemAttribute == null)
            {
                throw new InvalidOperationException("No rendering subsystem found. Are you missing assembly references?");
            }

            UseWindowingSubsystem(() => windowingSubsystemAttribute.InitializationType
                .GetRuntimeMethod(windowingSubsystemAttribute.InitializationMethod, Type.EmptyTypes).Invoke(null, null),
                windowingSubsystemAttribute.Name);

            UseRenderingSubsystem(() => renderingSubsystemAttribute.InitializationType
                .GetRuntimeMethod(renderingSubsystemAttribute.InitializationMethod, Type.EmptyTypes).Invoke(null, null),
                renderingSubsystemAttribute.Name);
            
            return this;
        }

        private void LoadAssembliesInDirectory()
        {
            var location = Assembly.GetEntryAssembly().Location;
            if (string.IsNullOrWhiteSpace(location))
                return;
            var dir = new FileInfo(location).Directory;
            if (dir == null)
                return;
            foreach (var file in dir.EnumerateFiles("*.dll"))
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
