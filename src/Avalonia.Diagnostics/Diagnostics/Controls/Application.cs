using System;
using Avalonia.Controls;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;
using App = Avalonia.Application;

namespace Avalonia.Diagnostics.Controls
{
    class Application : AvaloniaObject
       , Input.ICloseable

    {
        private readonly App _application;
        private static readonly Version s_version = typeof(AvaloniaObject).Assembly?.GetName()?.Version
            ?? Version.Parse("0.0.00");
        public event EventHandler? Closed;

        public Application(App application)
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
                Lifetimes.ISingleViewApplicationLifetime single => (single.MainView as Visual)?.VisualRoot?.Renderer,
                _ => null
            };
        }

        internal App Instance => _application;

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
        /// Gets the application's focus manager.
        /// </summary>
        /// <value>
        /// The application's focus manager.
        /// </value>
        public Input.IFocusManager? FocusManager =>
            _application.FocusManager;

        /// <summary>
        /// Gets the application's input manager.
        /// </summary>
        /// <value>
        /// The application's input manager.
        /// </value>
        public Input.InputManager? InputManager =>
            _application.InputManager;

        /// <summary>
        /// Gets the application clipboard.
        /// </summary>
        public Input.Platform.IClipboard? Clipboard =>
            _application.Clipboard;

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
    }
}
