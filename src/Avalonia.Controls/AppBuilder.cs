﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Media.Fonts;
using Avalonia.Media;

namespace Avalonia
{
    /// <summary>
    /// Initializes platform-specific services for an <see cref="Application"/>.
    /// </summary>
    public class AppBuilder
    {
        private static bool s_setupWasAlreadyCalled;
        private Action? _optionsInitializers;
        private Func<Application>? _appFactory;
        private IApplicationLifetime? _lifetime;
        
        /// <summary>
        /// Gets or sets the <see cref="IRuntimePlatform"/> instance.
        /// </summary>
        public IRuntimePlatform RuntimePlatform { get; set; }

        /// <summary>
        /// Gets or sets a method to call the initialize the runtime platform services (e. g. AssetLoader)
        /// </summary>
        public Action RuntimePlatformServicesInitializer { get; private set; }

        /// <summary>
        /// Gets the <see cref="Application"/> instance being initialized.
        /// </summary>
        public Application? Instance { get; private set; }
        
        /// <summary>
        /// Gets the type of the Instance (even if it's not created yet)
        /// </summary>
        public Type? ApplicationType { get; private set; }
        
        /// <summary>
        /// Gets or sets a method to call the initialize the windowing subsystem.
        /// </summary>
        public Action? WindowingSubsystemInitializer { get; private set; }

        /// <summary>
        /// Gets the name of the currently selected windowing subsystem.
        /// </summary>
        public string? WindowingSubsystemName { get; private set; }

        /// <summary>
        /// Gets or sets a method to call the initialize the windowing subsystem.
        /// </summary>
        public Action? RenderingSubsystemInitializer { get; private set; }

        /// <summary>
        /// Gets the name of the currently selected rendering subsystem.
        /// </summary>
        public string? RenderingSubsystemName { get; private set; }

        /// <summary>
        /// Gets or sets a method to call after the <see cref="Application"/> is setup.
        /// </summary>
        public Action<AppBuilder> AfterSetupCallback { get; private set; } = builder => { };


        public Action<AppBuilder> AfterPlatformServicesSetupCallback { get; private set; } = builder => { };
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AppBuilder"/> class.
        /// </summary>
        public AppBuilder()
            : this(new StandardRuntimePlatform(),
                builder => StandardRuntimePlatformServices.Register(builder.ApplicationType?.Assembly))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AppBuilder"/> class.
        /// </summary>
        protected AppBuilder(IRuntimePlatform platform, Action<AppBuilder> platformServices)
        {
            RuntimePlatform = platform;
            RuntimePlatformServicesInitializer = () => platformServices(this);
        }

        /// <summary>
        /// Begin configuring an <see cref="Application"/>.
        /// </summary>
        /// <typeparam name="TApp">The subclass of <see cref="Application"/> to configure.</typeparam>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public static AppBuilder Configure<TApp>()
            where TApp : Application, new()
        {
            return new AppBuilder()
            {
                ApplicationType = typeof(TApp),
                // Needed for CoreRT compatibility
                _appFactory = () => new TApp()
            };
        }

        /// <summary>
        /// Begin configuring an <see cref="Application"/>.
        /// </summary>
        /// <param name="appFactory">Factory function for <typeparamref name="TApp"/>.</param>
        /// <typeparam name="TApp">The subclass of <see cref="Application"/> to configure.</typeparam>
        /// <remarks><paramref name="appFactory"/> is useful for passing of dependencies to <typeparamref name="TApp"/>.</remarks>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public static AppBuilder Configure<TApp>(Func<TApp> appFactory)
            where TApp : Application
        {
            return new AppBuilder()
            {
                ApplicationType = typeof(TApp),
                _appFactory = appFactory
            };
        }

        protected AppBuilder Self => this;

        public AppBuilder AfterSetup(Action<AppBuilder> callback)
        {
            AfterSetupCallback = (Action<AppBuilder>)Delegate.Combine(AfterSetupCallback, callback);
            return Self;
        }
        
        
        public AppBuilder AfterPlatformServicesSetup(Action<AppBuilder> callback)
        {
            AfterPlatformServicesSetupCallback = (Action<AppBuilder>)Delegate.Combine(AfterPlatformServicesSetupCallback, callback);
            return Self;
        }

        public delegate void AppMainDelegate(Application app, string[] args);
        
        public void Start(AppMainDelegate main, string[] args)
        {
            Setup();
            main(Instance!, args);
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
        /// Sets up the platform-specific services for the application and initialized it with a particular lifetime, but does not run it.
        /// </summary>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public AppBuilder SetupWithLifetime(IApplicationLifetime lifetime)
        {
            _lifetime = lifetime;
            Setup();
            return Self;
        }
        
        /// <summary>
        /// Specifies a windowing subsystem to use.
        /// </summary>
        /// <param name="initializer">The method to call to initialize the windowing subsystem.</param>
        /// <param name="name">The name of the windowing subsystem.</param>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder UseWindowingSubsystem(Action initializer, string name = "")
        {
            WindowingSubsystemInitializer = initializer;
            WindowingSubsystemName = name;
            return Self;
        }

        /// <summary>
        /// Specifies a rendering subsystem to use.
        /// </summary>
        /// <param name="initializer">The method to call to initialize the rendering subsystem.</param>
        /// <param name="name">The name of the rendering subsystem.</param>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder UseRenderingSubsystem(Action initializer, string name = "")
        {
            RenderingSubsystemInitializer = initializer;
            RenderingSubsystemName = name;
            return Self;
        }

        /// <summary>
        /// Configures platform-specific options
        /// </summary>
        public AppBuilder With<T>(T options)
        {
            _optionsInitializers += () => { AvaloniaLocator.CurrentMutable.Bind<T>().ToConstant(options); };
            return Self;
        }
        
        /// <summary>
        /// Configures platform-specific options
        /// </summary>
        public AppBuilder With<T>(Func<T> options)
        {
            _optionsInitializers += () => { AvaloniaLocator.CurrentMutable.Bind<T>().ToFunc(options); };
            return Self;
        }
        
        /// <summary>
        /// Registers an action that is executed with the current font manager.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder ConfigureFonts(Action<FontManager> action)
        {
            return AfterSetup(appBuilder =>
            {
                action?.Invoke(FontManager.Current);
            });
        }

        /// <summary>
        /// Sets up the platform-specific services for the <see cref="Application"/>.
        /// </summary>
        private void Setup()
        {
            if (RuntimePlatformServicesInitializer == null)
            {
                throw new InvalidOperationException("No runtime platform services configured.");
            }

            if (WindowingSubsystemInitializer == null)
            {
                throw new InvalidOperationException("No windowing system configured.");
            }

            if (RenderingSubsystemInitializer == null)
            {
                throw new InvalidOperationException("No rendering system configured.");
            }

            if (_appFactory == null)
            {
                throw new InvalidOperationException("No Application factory configured.");
            }

            if (s_setupWasAlreadyCalled)
            {
                throw new InvalidOperationException("Setup was already called on one of AppBuilder instances");
            }

            s_setupWasAlreadyCalled = true;
            _optionsInitializers?.Invoke();
            RuntimePlatformServicesInitializer();
            RenderingSubsystemInitializer();
            WindowingSubsystemInitializer();
            AfterPlatformServicesSetupCallback(Self);
            Instance = _appFactory();
            Instance.ApplicationLifetime = _lifetime;
            AvaloniaLocator.CurrentMutable.BindToSelf(Instance);
            Instance.RegisterServices();
            Instance.Initialize();
            AfterSetupCallback(Self);
            Instance.OnFrameworkInitializationCompleted();
        }
    }
}
