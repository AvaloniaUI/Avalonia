// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using System.Threading;
using Perspex.Controls;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.Input.Platform;
using Perspex.Layout;
using Perspex.Rendering;
using Perspex.Styling;
using Perspex.Threading;

namespace Perspex
{
    /// <summary>
    /// Encapsulates a Perspex application.
    /// </summary>
    /// <remarks>
    /// The <see cref="Application"/> class encapsulates Perspex application-specific
    /// functionality, including:
    /// - A global set of <see cref="DataTemplates"/>.
    /// - A global set of <see cref="Styles"/>.
    /// - A <see cref="FocusManager"/>.
    /// - An <see cref="InputManager"/>.
    /// - Loads and initializes rendering and windowing subsystems with
    /// <see cref="InitializeSubsystems(int)"/> and <see cref="InitializeSubsystem(string)"/>.
    /// - Registers services needed by the rest of Perspex in the <see cref="RegisterServices"/>
    /// method.
    /// - Tracks the lifetime of the application.
    /// </remarks>
    public class Application : IGlobalDataTemplates, IGlobalStyles, IStyleRoot, IApplicationLifecycle
    {
        static Action _platformInitializationCallback;

        /// <summary>
        /// The application-global data templates.
        /// </summary>
        private DataTemplates _dataTemplates;

        private readonly Lazy<IClipboard> _clipboard =
            new Lazy<IClipboard>(() => (IClipboard)PerspexLocator.Current.GetService(typeof(IClipboard)));
        private readonly Styler _styler = new Styler();

        /// <summary>
        /// Initializes a new instance of the <see cref="Application"/> class.
        /// </summary>
        public Application()
        {
            if (Current != null)
            {
                throw new InvalidOperationException("Cannot create more than one Application instance.");
            }

            PerspexLocator.CurrentMutable.BindToSelf(this);
            OnExit += OnExiting;
        }

        /// <summary>
        /// Gets the current instance of the <see cref="Application"/> class.
        /// </summary>
        /// <value>
        /// The current instance of the <see cref="Application"/> class.
        /// </value>
        public static Application Current
        {
            get { return PerspexLocator.Current.GetService<Application>(); }
        }

        /// <summary>
        /// Gets or sets the application's global data templates.
        /// </summary>
        /// <value>
        /// The application's global data templates.
        /// </value>
        public DataTemplates DataTemplates
        {
            get { return _dataTemplates ?? (_dataTemplates = new DataTemplates()); }
            set { _dataTemplates = value; }
        }

        /// <summary>
        /// Gets the application's focus manager.
        /// </summary>
        /// <value>
        /// The application's focus manager.
        /// </value>
        public IFocusManager FocusManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the application's input manager.
        /// </summary>
        /// <value>
        /// The application's input manager.
        /// </value>
        public InputManager InputManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the application clipboard.
        /// </summary>
        public IClipboard Clipboard => _clipboard.Value;

        /// <summary>
        /// Gets the application's global styles.
        /// </summary>
        /// <value>
        /// The application's global styles.
        /// </value>
        /// <remarks>
        /// Global styles apply to all windows in the application.
        /// </remarks>
        public Styles Styles { get; } = new Styles();

        /// <summary>
        /// Gets the styling parent of the application, which is null.
        /// </summary>
        IStyleHost IStyleHost.StylingParent => null;

        public static void RegisterPlatformCallback(Action cb)
        {
            _platformInitializationCallback = cb;
        }

        /// <summary>
        /// Runs the application's main loop until the <see cref="ICloseable"/> is closed.
        /// </summary>
        /// <param name="closable">The closable to track</param>
        public void Run(ICloseable closable)
        {
            var source = new CancellationTokenSource();
            closable.Closed += OnExiting;
            closable.Closed += (s, e) => source.Cancel();
            Dispatcher.UIThread.MainLoop(source.Token);
        }

        /// <summary>
        /// Exits the application
        /// </summary>
        public void Exit()
        {
            OnExit?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// Sent when the application is exiting.
        /// </summary>
        public event EventHandler OnExit;


        /// <summary>
        /// Called when the application is exiting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnExiting(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Register's the services needed by Perspex.
        /// </summary>
        protected virtual void RegisterServices()
        {
            PerspexSynchronizationContext.InstallIfNeeded();
            FocusManager = new FocusManager();
            InputManager = new InputManager();

            PerspexLocator.CurrentMutable
                .Bind<IAccessKeyHandler>().ToTransient<AccessKeyHandler>()
                .Bind<IGlobalDataTemplates>().ToConstant(this)
                .Bind<IGlobalStyles>().ToConstant(this)
                .Bind<IFocusManager>().ToConstant(FocusManager)
                .Bind<IInputManager>().ToConstant(InputManager)
                .Bind<IKeyboardNavigationHandler>().ToTransient<KeyboardNavigationHandler>()
                .Bind<IStyler>().ToConstant(_styler)
                .Bind<ILayoutManager>().ToSingleton<LayoutManager>()
                .Bind<IRenderQueueManager>().ToTransient<RenderQueueManager>()
                .Bind<IApplicationLifecycle>().ToConstant(this);
        }

        /// <summary>
        /// Initializes the rendering and windowing subsystems according to platform.
        /// </summary>
        /// <param name="platformID">The value of Environment.OSVersion.Platform.</param>
        protected void InitializeSubsystems(int platformID)
        {
            if (_platformInitializationCallback != null)
            {
                _platformInitializationCallback();
            }
            else if (platformID == 4 || platformID == 6)
            {
                InitializeSubsystem("Perspex.Cairo");
                InitializeSubsystem("Perspex.Gtk");
            }
            else
            {
                InitializeSubsystem("Perspex.Direct2D1");
                InitializeSubsystem("Perspex.Win32");
            }
        }

        /// <summary>
        /// Initializes the rendering or windowing subsystem defined by the specified assemblt.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly.</param>
        protected static void InitializeSubsystem(string assemblyName)
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            var platformClassName = assemblyName.Replace("Perspex.", string.Empty) + "Platform";
            var platformClassFullName = assemblyName + "." + platformClassName;
            var platformClass = assembly.GetType(platformClassFullName);
            var init = platformClass.GetRuntimeMethod("Initialize", new Type[0]);
            init.Invoke(null, null);
        }

        internal static void InitializeWin32Subsystem()
        {
            InitializeSubsystem("Perspex.Direct2D1");
            InitializeSubsystem("Perspex.Win32");
        }
    }
}
