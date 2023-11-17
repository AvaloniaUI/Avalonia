using System;
using Avalonia.Controls;
using Avalonia.Styling;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Diagnostics.Controls
{
    internal class Application : TopLevelGroup
       , Input.ICloseable, IDisposable

    {
        private readonly Avalonia.Application _application;

        public event EventHandler? Closed;

        public static readonly StyledProperty<ThemeVariant?> RequestedThemeVariantProperty =
            ThemeVariantScope.RequestedThemeVariantProperty.AddOwner<Application>();
        
        public Application(ClassicDesktopStyleApplicationLifetimeTopLevelGroup group, Avalonia.Application application)
            : base(group)
        {
            _application = application;

            if (_application.ApplicationLifetime is Lifetimes.IControlledApplicationLifetime controller)
            {
                EventHandler<Lifetimes.ControlledApplicationLifetimeExitEventArgs> eh = default!;
                eh = (s, e) =>
                {
                    controller.Exit -= eh;
                    Closed?.Invoke(s, e);
                };
                controller.Exit += eh;
            }
            RendererRoot = application.ApplicationLifetime switch
            {
                Lifetimes.IClassicDesktopStyleApplicationLifetime classic => classic.MainWindow?.Renderer,
                Lifetimes.ISingleViewApplicationLifetime single => single.MainView?.VisualRoot?.Renderer,
                _ => null
            };

            SetCurrentValue(RequestedThemeVariantProperty, application.RequestedThemeVariant);
            _application.PropertyChanged += ApplicationOnPropertyChanged;
        }

        internal Avalonia.Application Instance => _application;

        /// <summary>
        /// Defines the <see cref="DataContext"/> property.
        /// </summary>
        public object? DataContext =>
            _application.DataContext;

        /// <summary>
        /// Gets or sets the application's global data templates.
        /// </summary>
        /// <value>
        /// The application's global data templates.
        /// </value>
        public Avalonia.Controls.Templates.DataTemplates DataTemplates =>
            _application.DataTemplates;

        /// <summary>
        /// Gets the application's input manager.
        /// </summary>
        /// <value>
        /// The application's input manager.
        /// </value>
        public Input.InputManager? InputManager =>
            _application.InputManager;

        /// <summary>
        /// Gets the application's global resource dictionary.
        /// </summary>
        public IResourceDictionary Resources =>
            _application.Resources;

        /// <summary>
        /// Gets the application's global styles.
        /// </summary>
        /// <value>
        /// The application's global styles.
        /// </value>
        /// <remarks>
        /// Global styles apply to all windows in the application.
        /// </remarks>
        public Styling.Styles Styles =>
            _application.Styles;

        /// <summary>
        /// Application lifetime, use it for things like setting the main window and exiting the app from code
        /// Currently supported lifetimes are:
        /// - <see cref="Lifetimes.IClassicDesktopStyleApplicationLifetime"/>
        /// - <see cref="Lifetimes.ISingleViewApplicationLifetime"/>
        /// - <see cref="Lifetimes.IControlledApplicationLifetime"/> 
        /// </summary>
        public Lifetimes.IApplicationLifetime? ApplicationLifetime =>
            _application.ApplicationLifetime;

        /// <summary>
        /// Application name to be used for various platform-specific purposes
        /// </summary>
        public string? Name =>
            _application.Name;

        /// <summary>
        /// Gets the root of the visual tree, if the control is attached to a visual tree.
        /// </summary>
        internal Rendering.IRenderer? RendererRoot { get; }
        
        /// <inheritdoc cref="ThemeVariantScope.RequestedThemeVariant" />
        public ThemeVariant? RequestedThemeVariant
        {
            get => GetValue(RequestedThemeVariantProperty);
            set => SetValue(RequestedThemeVariantProperty, value);
        }

        public void Dispose()
        {
            _application.PropertyChanged -= ApplicationOnPropertyChanged;
        }

        private void ApplicationOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Avalonia.Application.RequestedThemeVariantProperty)
            {
                SetCurrentValue(RequestedThemeVariantProperty, e.GetNewValue<ThemeVariant>());
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == RequestedThemeVariantProperty)
            {
                _application.RequestedThemeVariant = change.GetNewValue<ThemeVariant>();
            }
        }
    }
}
