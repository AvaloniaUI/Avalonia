using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Themes.Fluent
{
    public enum FluentThemeMode
    {
        Light,
        Dark,
    }

    public enum DensityStyle
    {
        Normal,
        Compact
    }

    /// <summary>
    /// Includes the fluent theme in an application.
    /// </summary>
    public class FluentTheme : AvaloniaObject, IStyle, IResourceProvider
    {
        private readonly Uri _baseUri;
        private Styles _fluentDark = new();
        private Styles _fluentLight = new();
        private Styles _sharedStyles = new();
        private Styles _densityStyles = new();
        private bool _isLoading;
        private IStyle? _loaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentTheme"/> class.
        /// </summary>
        /// <param name="baseUri">The base URL for the XAML context.</param>
        public FluentTheme(Uri baseUri)
        {
            _baseUri = baseUri;
            InitStyles(baseUri);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentTheme"/> class.
        /// </summary>
        /// <param name="serviceProvider">The XAML service provider.</param>
        public FluentTheme(IServiceProvider serviceProvider)
        {
            var ctx  = serviceProvider.GetService(typeof(IUriContext)) as IUriContext
                 ?? throw new NullReferenceException("Unable retrive UriContext");
            _baseUri = ctx.BaseUri;
            InitStyles(_baseUri);
        }

        public static readonly StyledProperty<FluentThemeMode> ModeProperty =
            AvaloniaProperty.Register<FluentTheme, FluentThemeMode>(nameof(Mode));

        public static readonly StyledProperty<DensityStyle> DensityStyleProperty =
            AvaloniaProperty.Register<FluentTheme, DensityStyle>(nameof(DensityStyle));

        /// <summary>
        /// Gets or sets the mode of the fluent theme (light, dark).
        /// </summary>
        public FluentThemeMode Mode
        {
            get => GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the density style of the fluent theme (normal, compact).
        /// </summary>
        public DensityStyle DensityStyle
        {
            get => GetValue(DensityStyleProperty);
            set => SetValue(DensityStyleProperty, value);
        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            
            if (_loaded is null)
            {
                // If style wasn't yet loaded, no need to change children styles,
                // it will be applied later in Loaded getter.
                return;
            }
            
            if (change.Property == ModeProperty)
            {
                if (Mode == FluentThemeMode.Dark)
                {
                    (Loaded as Styles)![1] = _fluentDark[0];
                    (Loaded as Styles)![2] = _fluentDark[1];
                }
                else
                {
                    (Loaded as Styles)![1] = _fluentLight[0];
                    (Loaded as Styles)![2] = _fluentLight[1];
                }
            }

            if (change.Property == DensityStyleProperty)
            {
                if (DensityStyle == DensityStyle.Compact)
                {
                    (Loaded as Styles)!.Add(_densityStyles[0]);
                }
                else if (DensityStyle == DensityStyle.Normal)
                {
                    (Loaded as Styles)!.Remove(_densityStyles[0]);
                }
            }
        }

        public IResourceHost? Owner => (Loaded as IResourceProvider)?.Owner;

        /// <summary>
        /// Gets the loaded style.
        /// </summary>
        public IStyle Loaded
        {
            get
            {
                if (_loaded == null)
                {
                    _isLoading = true;

                    if (Mode == FluentThemeMode.Light)
                    {
                        _loaded = new Styles() { _sharedStyles , _fluentLight[0], _fluentLight[1] };
                    }
                    else if (Mode == FluentThemeMode.Dark)
                    {
                        _loaded = new Styles() { _sharedStyles, _fluentDark[0], _fluentDark[1] };
                    }

                    if (DensityStyle == DensityStyle.Compact)
                    {
                        (_loaded as Styles)!.Add(_densityStyles[0]);
                    }

                    _isLoading = false;
                }

                return _loaded!;
            }
        }

        bool IResourceNode.HasResources => (Loaded as IResourceProvider)?.HasResources ?? false;

        IReadOnlyList<IStyle> IStyle.Children => _loaded?.Children ?? Array.Empty<IStyle>();

        public event EventHandler? OwnerChanged
        {
            add
            {
                if (Loaded is IResourceProvider rp)
                {
                    rp.OwnerChanged += value;
                }
            }
            remove
            {
                if (Loaded is IResourceProvider rp)
                {
                    rp.OwnerChanged -= value;
                }
            }
        }

        public SelectorMatchResult TryAttach(IStyleable target, IStyleHost? host) => Loaded.TryAttach(target, host);

        public bool TryGetResource(object key, out object? value)
        {
            if (!_isLoading && Loaded is IResourceProvider p)
            {
                return p.TryGetResource(key, out value);
            }

            value = null;
            return false;
        }

        void IResourceProvider.AddOwner(IResourceHost owner) => (Loaded as IResourceProvider)?.AddOwner(owner);
        void IResourceProvider.RemoveOwner(IResourceHost owner) => (Loaded as IResourceProvider)?.RemoveOwner(owner);

        private void InitStyles(Uri baseUri)
        {
            _sharedStyles = new Styles
            {
                new StyleInclude(baseUri)
                {
                    Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/AccentColors.xaml")
                },
                new StyleInclude(baseUri)
                {
                    Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
                },
                new StyleInclude(baseUri)
                {
                    Source = new Uri("avares://Avalonia.Themes.Fluent/Controls/FluentControls.xaml")
                }
            };

            _fluentLight = new Styles
            {
                new StyleInclude(baseUri)
                {
                    Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseLight.xaml")
                },
                new StyleInclude(baseUri)
                {
                    Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/FluentControlResourcesLight.xaml")
                }
            };

            _fluentDark = new Styles
            {
                new StyleInclude(baseUri)
                {
                    Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseDark.xaml")
                },
                new StyleInclude(baseUri)
                {
                    Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/FluentControlResourcesDark.xaml")
                }
            };
            
            _densityStyles = new Styles
            {
                new StyleInclude(baseUri)
                {
                    Source = new Uri("avares://Avalonia.Themes.Fluent/DensityStyles/Compact.xaml")
                }
            };
        }
    }
}
