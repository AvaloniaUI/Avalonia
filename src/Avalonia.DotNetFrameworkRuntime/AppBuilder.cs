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
                builder => StandardRuntimePlatformServices.Register(builder.Instance?.GetType()?.Assembly))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppBuilder"/> class.
        /// </summary>
        /// <param name="app">The <see cref="Application"/> instance.</param>
        public AppBuilder(Application app) : this()
        {
            Instance = app;
        }

        /// <summary>
        /// Instructs the <see cref="AppBuilder"/> to use the best settings for the platform.
        /// </summary>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder UsePlatformDetect()
        {
            var runtimeInfo = RuntimePlatform.GetRuntimeInfo();
            var os = runtimeInfo.OperatingSystem;

            LoadAssembliesInDirectory();

            var windowingSubsystemAttribute = (from assembly in RuntimePlatform.GetLoadedAssemblies()
                                               from attribute in assembly.GetCustomAttributes<ExportWindowingSubsystemAttribute>()
                                               where attribute.RequiredOS == os
                                               orderby attribute.Priority ascending
                                               select attribute).First();

            var renderingSubsystemAttributes = from assembly in RuntimePlatform.GetLoadedAssemblies()
                                               from attribute in assembly.GetCustomAttributes<ExportRenderingSubsystemAttribute>()
                                               where attribute.RequiredOS == os
                                               where attribute.RequiresWindowingSubsystem == null
                                                || attribute.RequiresWindowingSubsystem == windowingSubsystemAttribute.Name
                                               orderby attribute.Priority ascending
                                               select attribute;

            UseWindowingSubsystem(() => windowingSubsystemAttribute.InitializationType
                .GetRuntimeMethod(windowingSubsystemAttribute.InitializationMethod, Type.EmptyTypes).Invoke(null, null),
                windowingSubsystemAttribute.Name);

            if (!runtimeInfo.IsDirectXCompositionAvailable && renderingSubsystemAttributes.Count() > 1)
                renderingSubsystemAttributes = renderingSubsystemAttributes.Skip(1); // Not use DirectX for Windows < 8.1
            var renderingSubsystemAttribute = renderingSubsystemAttributes.First();

            UseRenderingSubsystem(() => renderingSubsystemAttribute.InitializationType
                .GetRuntimeMethod(renderingSubsystemAttribute.InitializationMethod, Type.EmptyTypes).Invoke(null, null),
                renderingSubsystemAttribute.Name);

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
