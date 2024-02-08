using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Media.Fonts;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia
{
    /// <summary>
    /// Initializes platform-specific services for an <see cref="Application"/>.
    /// </summary>
    public sealed class AppBuilder
    {
        private static bool s_setupWasAlreadyCalled;
        private Action? _optionsInitializers;
        private Func<Application>? _appFactory;
        private IApplicationLifetime? _lifetime;

        /// <summary>
        /// Gets or sets a method to call the initialize the runtime platform services (e. g. AssetLoader)
        /// </summary>
        public Action? RuntimePlatformServicesInitializer { get; private set; }

        /// <summary>
        /// Gets the name of the currently selected windowing subsystem.
        /// </summary>
        public string? RuntimePlatformServicesName { get; private set; }
        
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
        /// Gets a method to override a lifetime factory.
        /// </summary>
        public Func<Type, IApplicationLifetime?>? LifetimeOverride { get; private set; }

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
        private AppBuilder()
        {
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

        /// <summary>
        /// Begin configuring an <see cref="Application"/>.
        /// Should only be used for testing and design purposes, as it relies on dynamic code.
        /// </summary>
        /// <param name="entryPointType">
        /// Parameter from which <see cref="AppBuilder"/> should be created.
        /// It either needs to have BuildAvaloniaApp -> AppBuilder method or inherit Application.
        /// </param>
        /// <returns>An <see cref="AppBuilder"/> instance. If can't be created, thrown an exception.</returns>
        internal static AppBuilder Configure(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
            Type entryPointType)
        {
            var appBuilderObj = entryPointType
                .GetMethod(
                    "BuildAvaloniaApp",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy,
                    null,
                    Array.Empty<Type>(),
                    null)?
                .Invoke(null, Array.Empty<object?>());

            if (appBuilderObj is AppBuilder appBuilder)
            {
                return appBuilder;
            }

            if (typeof(Application).IsAssignableFrom(entryPointType))
            {
                return Configure(() => (Application)Activator.CreateInstance(entryPointType)!);
            }

            throw new InvalidOperationException(
                $"Unable to create AppBuilder from type \"{entryPointType.FullName}\". " +
                $"Input type either needs to have BuildAvaloniaApp -> AppBuilder method or inherit Application type.");
        }
        
        private AppBuilder Self => this;

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
        /// Specifies a runtime platform subsystem to use.
        /// </summary>
        /// <param name="initializer">The method to call to initialize the runtime platform subsystem.</param>
        /// <param name="name">The name of the runtime platform subsystem.</param>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder UseRuntimePlatformSubsystem(Action initializer, string name = "")
        {
            RuntimePlatformServicesInitializer = initializer;
            RuntimePlatformServicesName = name;
            return Self;
        }
        
        /// <summary>
        /// Specifies a standard runtime platform subsystem to use.
        /// </summary>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder UseStandardRuntimePlatformSubsystem()
        {
            RuntimePlatformServicesInitializer = () => StandardRuntimePlatformServices.Register(ApplicationType?.Assembly);
            RuntimePlatformServicesName = nameof(StandardRuntimePlatform);
            return Self;
        }

        [PrivateApi]
        public AppBuilder UseLifetimeOverride(Func<Type, IApplicationLifetime?> func)
        {
            LifetimeOverride = func;
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
            SetupUnsafe();
        }

        /// <summary>
        /// Setup method that doesn't check for input initalizers being set.
        /// Nor 
        /// </summary>
        internal void SetupUnsafe()
        {
            _optionsInitializers?.Invoke();
            RuntimePlatformServicesInitializer?.Invoke();
            RenderingSubsystemInitializer?.Invoke();
            WindowingSubsystemInitializer?.Invoke();
            AfterPlatformServicesSetupCallback?.Invoke(Self);
            Instance = _appFactory!();
            Instance.ApplicationLifetime = _lifetime;
            AvaloniaLocator.CurrentMutable.BindToSelf(Instance);
            Instance.RegisterServices();
            Instance.Initialize();
            AfterSetupCallback?.Invoke(Self);
            Instance.OnFrameworkInitializationCompleted();
        }
    }
}
