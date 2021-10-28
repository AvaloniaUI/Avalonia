using System;
using Avalonia.Controls;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;
using App = Avalonia.Application;
using RuntimeEnvironment = Microsoft.DotNet.PlatformAbstractions.RuntimeEnvironment;
using RuntimeInformation = System.Runtime.InteropServices.RuntimeInformation;
using System.Linq;
using System.Reflection;

namespace Avalonia.Diagnostics.Controls
{
    class Application : AvaloniaObject
       , Input.ICloseable

    {
        private readonly App _application;
        private static readonly Version s_version = typeof(IAvaloniaObject).Assembly.GetName().Version;
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
        public Input.IFocusManager FocusManager =>
            _application.FocusManager;


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
        /// Gets a string that describes the operating system on which the app is running. 
        /// </summary>
        public string OSVersion =>
            RuntimeInformation.OSDescription;

        /// <summary>
        /// Gets the platform on which an app is running.
        /// </summary>
        public string OSPlatform =>
            RuntimeEnvironment.OperatingSystemPlatform.ToString();

        /// <summary>
        /// 
        /// </summary>
        public string RuntimeId =>
            RuntimeEnvironment.GetRuntimeIdentifier();

        /// <summary>
        /// Get Product Version <see cref="AssemblyInformationalVersionAttribute"/>
        /// </summary>
        public string ProductVersion =>
            GetProductVersion();

        /// <summary>
        /// Get executable path
        /// </summary>
        public string EntryPoint =>
            Assembly.GetEntryAssembly().Location;

        /// <summary>
        /// Gets the name of the .NET installation on which an app is running.
        /// </summary>
        public string FrameworkVersion =>
            RuntimeInformation.FrameworkDescription;

        /// <summary>
        /// Get the Avalonia Framework Version on which an app is running.
        /// </summary>
        public string AvaloniaVersion { get; } = s_version.ToString(3);

        /// <summary>
        /// Get if the Avalonia Framework version an app is running on is Development Build.
        /// </summary>
        public bool IsDevelopmentBuild { get; } = s_version.Build == 999;

        /// <summary>
        /// Gets the directory that serves as a common repository for application-specific data for the current roaming user. A roaming user works on more than one computer on a network. A roaming user's profile is kept on a server on the network and is loaded onto a system when the user logs on.
        /// </summary>
        public string ApplicationData =>
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        /// <summary>
        /// Gets the directory that serves as a common repository for application-specific data that is used by the current, non-roaming user.
        /// </summary>
        public string LocalApplicationData =>
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        /// <summary>
        /// Gets or sets the fully qualified path of the current working directory.
        /// </summary>
        public string CurrentDirectory =>
            Environment.CurrentDirectory;

        /// <summary>
        /// Get the command-line arguments for the current process without executable file name.
        /// </summary>
        public string CommandLineArgs =>
            string.Join(Environment.NewLine, Environment.GetCommandLineArgs().Skip(1));


        private string GetProductVersion()
        {

            if (Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute))
                .FirstOrDefault()
                is AssemblyInformationalVersionAttribute attribute)
            {
                return attribute.InformationalVersion;
            }

            return "Unknown";
        }
        
    }
}
