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


        public Lifetimes.IApplicationLifetime? ApplicationLifetime =>
            _application.ApplicationLifetime;

        /// <summary>
        /// Application name to be used for various platform-specific purposes
        /// </summary>
        public string? Name =>
            _application.Name;

        internal App Instance => _application;

        public string OSVersion =>
            RuntimeInformation.OSDescription;

        public string OSPlatform =>
            RuntimeEnvironment.OperatingSystemPlatform.ToString();

        public string RuntimeId =>
            RuntimeEnvironment.GetRuntimeIdentifier();

        public string ProductVersion =>
            GetProductVersion();

        public string EntryPoint =>
            Assembly.GetEntryAssembly().Location;

        public string FrameworkVersion =>
            RuntimeInformation.FrameworkDescription;

        public string AvaloniaVersion { get; } = s_version.ToString(3);

        public bool IsDevelopmentBuild { get; } = s_version.Build == 999;

        public string ApplicationData =>
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        public string LocalApplicationData =>
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public string CurrentDirectory =>
            Environment.CurrentDirectory;

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

        public static string GetRunningFrameworkVersion()
        {
            var netVer = Environment.Version.ToString();
            var assObj = typeof(object).GetTypeInfo().Assembly;
            if (assObj != null)
            {

                var attr = (AssemblyFileVersionAttribute)assObj.GetCustomAttribute(typeof(AssemblyFileVersionAttribute));
                if (attr != null)
                {
                    netVer = attr.Version;
                }
            }
            return netVer;
        }
    }
}
