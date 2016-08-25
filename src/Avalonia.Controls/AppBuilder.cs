// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using Avalonia.Platform;

namespace Avalonia.Controls
{
    /// <summary>
    /// Initializes up platform-specific services for an <see cref="Application"/>.
    /// </summary>
    public abstract class AppBuilderBase<AppBuilder> where AppBuilder : AppBuilderBase<AppBuilder>, new()
    {
        /// <summary>
        /// Gets or sets the <see cref="IRuntimePlatform"/> instance.
        /// </summary>
        public IRuntimePlatform RuntimePlatform { get; set; }

        /// <summary>
        /// Gets or sets a method to call the initialize the runtime platform services (e. g. AssetLoader)
        /// </summary>
        public Action RuntimePlatformServices { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Application"/> instance being initialized.
        /// </summary>
        public Application Instance { get; set; }

        /// <summary>
        /// Gets or sets a method to call the initialize the windowing subsystem.
        /// </summary>
        public Action WindowingSubsystem { get; set; }

        /// <summary>
        /// Gets or sets a method to call the initialize the windowing subsystem.
        /// </summary>
        public Action RenderingSubsystem { get; set; }

        /// <summary>
        /// Gets or sets a method to call before <see cref="Start{TMainWindow}"/> is called on the
        /// <see cref="Application"/>.
        /// </summary>
        public Action<AppBuilder> BeforeStartCallback { get; set; }

        protected AppBuilderBase(IRuntimePlatform platform, Action platformSevices)
        {
            RuntimePlatform = platform;
            RuntimePlatformServices = platformSevices;
        }

        /// <summary>
        /// Begin configuring an <see cref="Application"/>.
        /// </summary>
        /// <typeparam name="TApp">The subclass of <see cref="Application"/> to configure.</typeparam>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public static AppBuilder Configure<TApp>()
            where TApp : Application, new()
        {
            return Configure(new TApp());
        }

        /// <summary>
        /// Begin configuring an <see cref="Application"/>.
        /// </summary>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public static AppBuilder Configure(Application app)
        {
            AvaloniaLocator.CurrentMutable.BindToSelf(app);

            return new AppBuilder()
            {
                Instance = app,
            };
        }

        protected AppBuilder Self => (AppBuilder) this;

        /// <summary>
        /// Registers a callback to call before <see cref="Start{TMainWindow}"/> is called on the
        /// <see cref="Application"/>.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder BeforeStarting(Action<AppBuilder> callback)
        {
            BeforeStartCallback = callback;
            return Self;
        }

        /// <summary>
        /// Starts the application with an instance of <typeparamref name="TMainWindow"/>.
        /// </summary>
        /// <typeparam name="TMainWindow">The window type.</typeparam>
        public void Start<TMainWindow>()
            where TMainWindow : Window, new()
        {
            Setup();
            BeforeStartCallback?.Invoke(Self);

            var window = new TMainWindow();
            window.Show();
            Instance.Run(window);
        }

        /// <summary>
        /// Sets up the platform-specific services for the application, but does not run it.
        /// </summary>
        /// <returns></returns>
        public AppBuilder SetupWithoutStarting()
        {
            Setup();
            return Self;
        }

        /// <summary>
        /// Specifies a windowing subsystem to use.
        /// </summary>
        /// <param name="initializer">The method to call to initialize the windowing subsystem.</param>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder UseWindowingSubsystem(Action initializer)
        {
            WindowingSubsystem = initializer;
            return Self;
        }

        /// <summary>
        /// Specifies a windowing subsystem to use.
        /// </summary>
        /// <param name="dll">The dll in which to look for subsystem.</param>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder UseWindowingSubsystem(string dll) => UseWindowingSubsystem(GetInitializer(dll));

        /// <summary>
        /// Specifies a rendering subsystem to use.
        /// </summary>
        /// <param name="initializer">The method to call to initialize the rendering subsystem.</param>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder UseRenderingSubsystem(Action initializer)
        {
            RenderingSubsystem = initializer;
            return Self;
        }

        /// <summary>
        /// Specifies a rendering subsystem to use.
        /// </summary>
        /// <param name="dll">The dll in which to look for subsystem.</param>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder UseRenderingSubsystem(string dll) => UseRenderingSubsystem(GetInitializer(dll));

        static Action GetInitializer(string assemblyName) => () =>
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            var platformClassName = assemblyName.Replace("Avalonia.", string.Empty) + "Platform";
            var platformClassFullName = assemblyName + "." + platformClassName;
            var platformClass = assembly.GetType(platformClassFullName);
            var init = platformClass.GetRuntimeMethod("Initialize", new Type[0]);
            init.Invoke(null, null);
        };

        /// <summary>
        /// Sets up the platform-speciic services for the <see cref="Application"/>.
        /// </summary>
        private void Setup()
        {
            if (Instance == null)
            {
                throw new InvalidOperationException("No App instance configured.");
            }

            if (RuntimePlatformServices == null)
            {
                throw new InvalidOperationException("No runtime platform services configured.");
            }

            if (WindowingSubsystem == null)
            {
                throw new InvalidOperationException("No windowing system configured.");
            }

            if (RenderingSubsystem == null)
            {
                throw new InvalidOperationException("No rendering system configured.");
            }

            Instance.RegisterServices();
            RuntimePlatformServices();
            WindowingSubsystem();
            RenderingSubsystem();
            Instance.Initialize();
        }
    }
}
