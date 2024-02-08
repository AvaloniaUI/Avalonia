using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Media;
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
    public class Application : AvaloniaObject, IDataContextProvider, IGlobalDataTemplates, IGlobalStyles, IThemeVariantHost, IApplicationPlatformEvents
    {
        /// <summary>
        /// The application-global data templates.
        /// </summary>
        private DataTemplates? _dataTemplates;

        private Styles? _styles;
        private IResourceDictionary? _resources;
        private bool _notifyingResourcesChanged;
        private Action<IReadOnlyList<IStyle>>? _stylesAdded;
        private Action<IReadOnlyList<IStyle>>? _stylesRemoved;
        private IApplicationLifetime? _applicationLifetime;
        private bool _setupCompleted;

        /// <summary>
        /// Defines the <see cref="DataContext"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> DataContextProperty =
            StyledElement.DataContextProperty.AddOwner<Application>();

        /// <inheritdoc cref="ThemeVariantScope.ActualThemeVariantProperty" />
        public static readonly StyledProperty<ThemeVariant> ActualThemeVariantProperty =
            ThemeVariantScope.ActualThemeVariantProperty.AddOwner<Application>();
        
        /// <inheritdoc cref="ThemeVariantScope.RequestedThemeVariantProperty" />
        public static readonly StyledProperty<ThemeVariant?> RequestedThemeVariantProperty =
            ThemeVariantScope.RequestedThemeVariantProperty.AddOwner<Application>();

        /// <inheritdoc/>
        public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;

        [Obsolete("Cast ApplicationLifetime to IActivatableApplicationLifetime instead.")]
        public event EventHandler<UrlOpenedEventArgs>? UrlsOpened;

        /// <inheritdoc/>
        public event EventHandler? ActualThemeVariantChanged;

        /// <summary>
        /// Creates an instance of the <see cref="Application"/> class.
        /// </summary>
        public Application()
        {
            Name = "Avalonia Application";
        }

        /// <summary>
        /// Gets or sets the Applications's data context.
        /// </summary>
        /// <remarks>
        /// The data context property specifies the default object that will
        /// be used for data binding.
        /// </remarks>
        public object? DataContext
        {
            get => GetValue(DataContextProperty);
            set => SetValue(DataContextProperty, value);
        }

        /// <inheritdoc cref="ThemeVariantScope.RequestedThemeVariant"/>
        public ThemeVariant? RequestedThemeVariant
        {
            get => GetValue(RequestedThemeVariantProperty);
            set => SetValue(RequestedThemeVariantProperty, value);
        }
        
        /// <inheritdoc />
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1031", Justification = "This property is supposed to be a styled readonly property.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1030", Justification = "False positive.")]
        public ThemeVariant ActualThemeVariant => GetValue(ActualThemeVariantProperty);

        /// <summary>
        /// Gets the current instance of the <see cref="Application"/> class.
        /// </summary>
        /// <value>
        /// The current instance of the <see cref="Application"/> class.
        /// </value>
        public static Application? Current
        {
            get => AvaloniaLocator.Current.GetService<Application>();
        }

        /// <summary>
        /// Gets or sets the application's global data templates.
        /// </summary>
        /// <value>
        /// The application's global data templates.
        /// </value>
        public DataTemplates DataTemplates => _dataTemplates ?? (_dataTemplates = new DataTemplates());

        /// <summary>
        /// Gets the application's input manager.
        /// </summary>
        /// <value>
        /// The application's input manager.
        /// </value>
        internal InputManager? InputManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the application's global resource dictionary.
        /// </summary>
        public IResourceDictionary Resources
        {
            get => _resources ??= new ResourceDictionary(this);
            set
            {
                value = value ?? throw new ArgumentNullException(nameof(value));
                _resources?.RemoveOwner(this);
                _resources = value;
                _resources.AddOwner(this);
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
        public Styles Styles => _styles ??= new Styles(this);

        /// <inheritdoc/>
        bool IDataTemplateHost.IsDataTemplatesInitialized => _dataTemplates != null;

        /// <inheritdoc/>
        bool IResourceNode.HasResources => (_resources?.HasResources ?? false) ||
            (((IResourceNode?)_styles)?.HasResources ?? false);

        /// <summary>
        /// Gets the styling parent of the application, which is null.
        /// </summary>
        IStyleHost? IStyleHost.StylingParent => null;

        /// <inheritdoc/>
        bool IStyleHost.IsStylesInitialized => _styles != null;

        /// <summary>
        /// Application lifetime, use it for things like setting the main window and exiting the app from code
        /// Currently supported lifetimes are:
        /// - <see cref="IClassicDesktopStyleApplicationLifetime"/>
        /// - <see cref="ISingleViewApplicationLifetime"/>
        /// - <see cref="IControlledApplicationLifetime"/> 
        /// - <see cref="IActivatableApplicationLifetime"/> 
        /// </summary>
        public IApplicationLifetime? ApplicationLifetime
        {
            get => _applicationLifetime;
            set
            {
                if (_setupCompleted)
                {
                    throw new InvalidOperationException($"It's not possible to change {nameof(ApplicationLifetime)} after Application was initialized.");
                }

                _applicationLifetime = value;
            }
        }

        /// <summary>
        /// Represents a contract for accessing global platform-specific settings.
        /// </summary>
        /// <remarks>
        /// PlatformSettings can be null only if application wasn't initialized yet.
        /// <see cref="TopLevel"/>'s <see cref="TopLevel.PlatformSettings"/> is an equivalent API
        /// which should always be preferred over a global one,
        /// as specific top levels might have different settings set-up. 
        /// </remarks>
        public IPlatformSettings? PlatformSettings => AvaloniaLocator.Current.GetService<IPlatformSettings>();
        
        event Action<IReadOnlyList<IStyle>>? IGlobalStyles.GlobalStylesAdded
        {
            add => _stylesAdded += value;
            remove => _stylesAdded -= value;
        }

        event Action<IReadOnlyList<IStyle>>? IGlobalStyles.GlobalStylesRemoved
        {
            add => _stylesRemoved += value;
            remove => _stylesRemoved -= value;
        }

        /// <summary>
        /// Initializes the application by loading XAML etc.
        /// </summary>
        public virtual void Initialize() { }
        
        /// <inheritdoc/>
        public bool TryGetResource(object key, ThemeVariant? theme, out object? value)
        {
            value = null;
            return (_resources?.TryGetResource(key, theme, out value) ?? false) ||
                   Styles.TryGetResource(key, theme, out value);
        }

        void IResourceHost.NotifyHostedResourcesChanged(ResourcesChangedEventArgs e)
        {
            ResourcesChanged?.Invoke(this, e);
        }

        void IStyleHost.StylesAdded(IReadOnlyList<IStyle> styles)
        {
            _stylesAdded?.Invoke(styles);
        }

        void IStyleHost.StylesRemoved(IReadOnlyList<IStyle> styles)
        {
            _stylesRemoved?.Invoke(styles);
        }

        /// <summary>
        /// Register's the services needed by Avalonia.
        /// </summary>
        public virtual void RegisterServices()
        {
            AvaloniaSynchronizationContext.InstallIfNeeded();
            var focusManager = new FocusManager();
            InputManager = new InputManager();

            if (PlatformSettings is { } settings)
            {
                settings.ColorValuesChanged += OnColorValuesChanged;
                OnColorValuesChanged(settings, settings.GetColorValues());
            }

            AvaloniaLocator.CurrentMutable
                .Bind<IAccessKeyHandler>().ToTransient<AccessKeyHandler>()
                .Bind<IGlobalDataTemplates>().ToConstant(this)
                .Bind<IGlobalStyles>().ToConstant(this)
                .Bind<IThemeVariantHost>().ToConstant(this)
                .Bind<IFocusManager>().ToConstant(focusManager)
                .Bind<IInputManager>().ToConstant(InputManager)
                .Bind<IKeyboardNavigationHandler>().ToTransient<KeyboardNavigationHandler>()
                .Bind<IDragDropDevice>().ToConstant(DragDropDevice.Instance);

            // TODO: Fix this, for now we keep this behavior since someone might be relying on it in 0.9.x
            if (AvaloniaLocator.Current.GetService<IPlatformDragSource>() == null)
                AvaloniaLocator.CurrentMutable
                    .Bind<IPlatformDragSource>().ToTransient<InProcessDragSource>();

            AvaloniaLocator.CurrentMutable.Bind<IGlobalClock>()
                .ToConstant(MediaContext.Instance.Clock);

            _setupCompleted = true;
        }

        public virtual void OnFrameworkInitializationCompleted()
        {
        }
        
        void IApplicationPlatformEvents.RaiseUrlsOpened(string[] urls)
        {
            UrlsOpened?.Invoke(this, new UrlOpenedEventArgs (urls));
        }

        private void NotifyResourcesChanged(ResourcesChangedEventArgs e)
        {
            if (_notifyingResourcesChanged)
            {
                return;
            }

            try
            {
                _notifyingResourcesChanged = true;
                ResourcesChanged?.Invoke(this, ResourcesChangedEventArgs.Empty);
            }
            finally
            {
                _notifyingResourcesChanged = false;
            }
        }

        private void ThisResourcesChanged(object sender, ResourcesChangedEventArgs e)
        {
            NotifyResourcesChanged(e);
        }

        private string? _name;
        /// <summary>
        /// Defines Name property
        /// </summary>
        public static readonly DirectProperty<Application, string?> NameProperty =
            AvaloniaProperty.RegisterDirect<Application, string?>("Name", o => o.Name, (o, v) => o.Name = v);

        /// <summary>
        /// Application name to be used for various platform-specific purposes
        /// </summary>
        public string? Name
        {
            get => _name;
            set => SetAndRaise(NameProperty, ref _name, value);
        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == RequestedThemeVariantProperty)
            {
                if (change.GetNewValue<ThemeVariant>() is {} themeVariant && themeVariant != ThemeVariant.Default)
                    SetValue(ActualThemeVariantProperty, themeVariant);
                else
                    ClearValue(ActualThemeVariantProperty);
            }
            else if (change.Property == ActualThemeVariantProperty)
            {
                ActualThemeVariantChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private void OnColorValuesChanged(object? sender, PlatformColorValues e)
        {
            SetValue(ActualThemeVariantProperty, (ThemeVariant)e.ThemeVariant, BindingPriority.Template);
        }
    }
}
