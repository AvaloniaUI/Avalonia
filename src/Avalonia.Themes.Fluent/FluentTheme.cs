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

    /// <summary>
    /// Includes the fluent theme in an application.
    /// </summary>
    public class FluentTheme : IStyle, IResourceProvider
    {
        private readonly Uri _baseUri;
        private IStyle[]? _loaded;
        private bool _isLoading;
        private FluentThemeMode _mode;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentTheme"/> class.
        /// </summary>
        /// <param name="baseUri">The base URL for the XAML context.</param>
        public FluentTheme(Uri baseUri)
        {
            _baseUri = baseUri;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentTheme"/> class.
        /// </summary>
        /// <param name="serviceProvider">The XAML service provider.</param>
        public FluentTheme(IServiceProvider serviceProvider)
        {
            _baseUri = ((IUriContext)serviceProvider.GetService(typeof(IUriContext))).BaseUri;
        }

        /// <summary>
        /// Gets or sets the mode of the fluent theme (light, dark).
        /// </summary>
        public FluentThemeMode Mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    _mode = value;
                    (Loaded as Styles)[3] = FluentDark[0];
                    (Loaded as Styles)[4] = FluentDark[1];
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
                    Styles? resultStyle = new Styles();

                    resultStyle.AddRange(SharedStyles);

                    if (Mode == FluentThemeMode.Light)
                    {
                        for (int i = 0; i < FluentLight.Count; i++)
                        {
                            resultStyle.Add(FluentLight[i]);
                        }
                    }
                    else if (Mode == FluentThemeMode.Dark)
                    {
                        for (int i = 0; i < FluentDark.Count; i++)
                        {
                            resultStyle.Add(FluentDark[i]);
                        }
                    }
                    _loaded = new[] { resultStyle };
                    _isLoading = false;
                }

                return _loaded?[0]!;
            }
        }

        bool IResourceNode.HasResources => (Loaded as IResourceProvider)?.HasResources ?? false;

        IReadOnlyList<IStyle> IStyle.Children => _loaded ?? Array.Empty<IStyle>();

        public event EventHandler OwnerChanged
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

        private static Styles SharedStyles = new Styles
        {
            new StyleInclude(new Uri("resm:Styles?assembly=Avalonia.Themes.Fluent"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/AccentColors.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=Avalonia.Themes.Fluent"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=Avalonia.Themes.Fluent"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Controls/FluentControls.xaml")
            }
        };

        private static Styles FluentLight = new Styles
        {
            new StyleInclude(new Uri("resm:Styles?assembly=Avalonia.Themes.Fluent"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseLight.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=Avalonia.Themes.Fluent"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/FluentControlResourcesLight.xaml")
            }
        };
        private static Styles FluentDark = new Styles
        {
            new StyleInclude(new Uri("resm:Styles?assembly=Avalonia.Themes.Fluent"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseDark.xaml")
            },
            new StyleInclude(new Uri("resm:Styles?assembly=Avalonia.Themes.Fluent"))
            {
                Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/FluentControlResourcesDark.xaml")
            }
        };
    }
}
