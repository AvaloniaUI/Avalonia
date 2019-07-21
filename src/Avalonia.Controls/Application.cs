// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Threading;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
    public class Application : IGlobalDataTemplates, IGlobalStyles, IStyleRoot, IResourceNode
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
        /// Application lifetime, use it for things like setting the main window and exiting the app from code
        /// Currently supported lifetimes are:
        /// - <see cref="IClassicDesktopStyleApplicationLifetime"/>
        /// - <see cref="ISingleViewApplicationLifetime"/>
        /// - <see cref="IControlledApplicationLifetime"/> 
        /// </summary>
        public IApplicationLifetime ApplicationLifetime { get; set; }

        /// <summary>
        /// Initializes the application by loading XAML etc.
        /// </summary>
        public virtual void Initialize() { }

        /// <inheritdoc/>
        bool IResourceProvider.TryGetResource(object key, out object value)
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
                .Bind<IScheduler>().ToConstant(AvaloniaScheduler.Instance)
                .Bind<IDragDropDevice>().ToConstant(DragDropDevice.Instance)
                .Bind<IPlatformDragSource>().ToTransient<InProcessDragSource>();

            var clock = new RenderLoopClock();
            AvaloniaLocator.CurrentMutable
                .Bind<IGlobalClock>().ToConstant(clock)
                .GetService<IRenderLoop>()?.Add(clock);
        }

        public virtual void OnFrameworkInitializationCompleted()
        {
            
        }

        private void ThisResourcesChanged(object sender, ResourcesChangedEventArgs e)
        {
            ResourcesChanged?.Invoke(this, e);
        }
    }
}
