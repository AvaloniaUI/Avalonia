// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Threading;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Avalonia
{
    /// <summary>
    /// Encapsulates a Avalonia application.
    /// </summary>
    /// <remarks>
    /// The <see cref="Application"/> class encapsulates Avalonia application-specific
    /// functionality, including:
    /// - A global set of <see cref="DataTemplates"/>.
    /// - A global set of <see cref="Styles"/>.
    /// - A <see cref="FocusManager"/>.
    /// - An <see cref="InputManager"/>.
    /// - Registers services needed by the rest of Avalonia in the <see cref="RegisterServices"/>
    /// method.
    /// - Tracks the lifetime of the application.
    /// </remarks>
    public class Application : IApplicationLifecycle, IGlobalDataTemplates, IGlobalStyles, IStyleRoot, IResourceNode
    {
        /// <summary>
        /// The application-global data templates.
        /// </summary>
        private DataTemplates _dataTemplates;

        private readonly Lazy<IClipboard> _clipboard =
            new Lazy<IClipboard>(() => (IClipboard)AvaloniaLocator.Current.GetService(typeof(IClipboard)));
        private readonly Styler _styler = new Styler();
        private Styles _styles;
        private IResourceDictionary _resources;
        private CancellationTokenSource _mainLoopCancellationTokenSource;
        private int _exitCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="Application"/> class.
        /// </summary>
        public Application()
        {
            Windows = new WindowCollection(this);
        }

        /// <inheritdoc/>
        public event EventHandler<StartupEventArgs> Startup;

        /// <inheritdoc/>
        public event EventHandler<ExitEventArgs> Exit;

        /// <inheritdoc/>
        public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        /// <summary>
        /// Gets the current instance of the <see cref="Application"/> class.
        /// </summary>
        /// <value>
        /// The current instance of the <see cref="Application"/> class.
        /// </value>
        public static Application Current
        {
            get { return AvaloniaLocator.Current.GetService<Application>(); }
        }

        /// <summary>
        /// Gets or sets the application's global data templates.
        /// </summary>
        /// <value>
        /// The application's global data templates.
        /// </value>
        public DataTemplates DataTemplates => _dataTemplates ?? (_dataTemplates = new DataTemplates());

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
        /// Gets the application's global resource dictionary.
        /// </summary>
        public IResourceDictionary Resources
        {
            get => _resources ?? (Resources = new ResourceDictionary());
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);

                var hadResources = false;

                if (_resources != null)
                {
                    hadResources = _resources.Count > 0;
                    _resources.ResourcesChanged -= ThisResourcesChanged;
                }

                _resources = value;
                _resources.ResourcesChanged += ThisResourcesChanged;

                if (hadResources || _resources.Count > 0)
                {
                    ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs());
                }
            }
        }

        /// <summary>
        /// Gets the application's global styles.
        /// </summary>
        /// <value>
        /// The application's global styles.
        /// </value>
        /// <remarks>
        /// Global styles apply to all windows in the application.
        /// </remarks>
        public Styles Styles => _styles ?? (_styles = new Styles());

        /// <inheritdoc/>
        bool IDataTemplateHost.IsDataTemplatesInitialized => _dataTemplates != null;

        /// <summary>
        /// Gets the styling parent of the application, which is null.
        /// </summary>
        IStyleHost IStyleHost.StylingParent => null;

        /// <inheritdoc/>
        bool IStyleHost.IsStylesInitialized => _styles != null;

        /// <inheritdoc/>
        bool IResourceProvider.HasResources => _resources?.Count > 0;

        /// <inheritdoc/>
        IResourceNode IResourceNode.ResourceParent => null;

        /// <summary>
        /// Gets or sets the <see cref="ShutdownMode"/>. This property indicates whether the application is shutdown explicitly or implicitly. 
        /// If <see cref="ShutdownMode"/> is set to OnExplicitShutdown the application is only closes if Shutdown is called.
        /// The default is OnLastWindowClose
        /// </summary>
        /// <value>
        /// The shutdown mode.
        /// </value>
        public ShutdownMode ShutdownMode { get; set; }

        /// <summary>
        /// Gets or sets the main window of the application.
        /// </summary>
        /// <value>
        /// The main window.
        /// </value>
        public Window MainWindow { get; set; }

        /// <summary>
        /// Gets the open windows of the application.
        /// </summary>
        /// <value>
        /// The windows.
        /// </value>
        public WindowCollection Windows { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is shutting down.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is shutting down; otherwise, <c>false</c>.
        /// </value>
        internal bool IsShuttingDown { get; private set; }

        /// <summary>
        /// Initializes the application by loading XAML etc.
        /// </summary>
        public virtual void Initialize() { }

        public int Run()
        {
            return Run(new CancellationTokenSource());
        }

        /// <summary>
        /// Runs the application's main loop until the <see cref="ICloseable"/> is closed.
        /// </summary>
        /// <param name="closable">The closable to track</param>
        public int Run(ICloseable closable)
        {
            closable.Closed += (s, e) => _mainLoopCancellationTokenSource?.Cancel();

            return Run(new CancellationTokenSource());
        }

        /// <summary>
        /// Runs the application's main loop until some condition occurs that is specified by ExitMode.
        /// </summary>
        /// <param name="mainWindow">The main window</param>
        public int Run(Window mainWindow)
        {
            if (mainWindow == null)
            {
                throw new ArgumentNullException(nameof(mainWindow));
            }

            if (MainWindow == null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (!mainWindow.IsVisible)
                    {
                        mainWindow.Show();
                    }

                    MainWindow = mainWindow;
                });
            }            

            return Run(new CancellationTokenSource());
        }

        /// <summary>
        /// Runs the application's main loop until the <see cref="CancellationToken"/> is canceled.
        /// </summary>
        /// <param name="token">The token to track</param>
        public int Run(CancellationToken token)
        {
            return Run(CancellationTokenSource.CreateLinkedTokenSource(token));
        }

        private int Run(CancellationTokenSource tokenSource)
        {
            if (IsShuttingDown)
            {
                throw new InvalidOperationException("Application is shutting down.");
            }

            if (_mainLoopCancellationTokenSource != null)
            {
                throw new InvalidOperationException("Application is already running.");
            }

            _mainLoopCancellationTokenSource = tokenSource;

            Dispatcher.UIThread.Post(() => OnStartup(new StartupEventArgs()), DispatcherPriority.Send);

            Dispatcher.UIThread.MainLoop(_mainLoopCancellationTokenSource.Token);

            if (!IsShuttingDown)
            {
                Shutdown(_exitCode);
            }

            return _exitCode;
        }

        protected virtual void OnStartup(StartupEventArgs e)
        {
            Startup?.Invoke(this, e);
        }

        protected virtual void OnExit(ExitEventArgs e)
        {
            Exit?.Invoke(this, e);
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            Shutdown(0);
        }

        /// <inheritdoc/>
        public void Shutdown(int exitCode)
        {
            if (IsShuttingDown)
            {
                throw new InvalidOperationException("Application is already shutting down.");
            }

            _exitCode = exitCode;

            IsShuttingDown = true;         

            Windows.Clear();

            try
            {
                var e = new ExitEventArgs { ApplicationExitCode = _exitCode };

                OnExit(e);

                _exitCode = e.ApplicationExitCode;

                Environment.ExitCode = _exitCode;
            }
            finally
            {
                _mainLoopCancellationTokenSource?.Cancel();

                _mainLoopCancellationTokenSource = null;

                IsShuttingDown = false;
            }
        }

        /// <inheritdoc/>
        bool IResourceProvider.TryGetResource(string key, out object value)
        {
            value = null;
            return (_resources?.TryGetResource(key, out value) ?? false) ||
                   Styles.TryGetResource(key, out value);
        }

        /// <summary>
        /// Register's the services needed by Avalonia.
        /// </summary>
        public virtual void RegisterServices()
        {
            AvaloniaSynchronizationContext.InstallIfNeeded();
            FocusManager = new FocusManager();
            InputManager = new InputManager();

            AvaloniaLocator.CurrentMutable
                .Bind<IAccessKeyHandler>().ToTransient<AccessKeyHandler>()
                .Bind<IGlobalDataTemplates>().ToConstant(this)
                .Bind<IGlobalStyles>().ToConstant(this)
                .Bind<IFocusManager>().ToConstant(FocusManager)
                .Bind<IInputManager>().ToConstant(InputManager)
                .Bind<IKeyboardNavigationHandler>().ToTransient<KeyboardNavigationHandler>()
                .Bind<IStyler>().ToConstant(_styler)
                .Bind<IApplicationLifecycle>().ToConstant(this)
                .Bind<IScheduler>().ToConstant(AvaloniaScheduler.Instance)
                .Bind<IDragDropDevice>().ToConstant(DragDropDevice.Instance)
                .Bind<IPlatformDragSource>().ToTransient<InProcessDragSource>();

            var clock = new RenderLoopClock();
            AvaloniaLocator.CurrentMutable
                .Bind<IGlobalClock>().ToConstant(clock)
                .GetService<IRenderLoop>()?.Add(clock);
        }

        private void ThisResourcesChanged(object sender, ResourcesChangedEventArgs e)
        {
            ResourcesChanged?.Invoke(this, e);
        }
    }
}
