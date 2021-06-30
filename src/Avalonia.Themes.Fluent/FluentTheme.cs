using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
    public class FluentTheme : IStyle, IResourceProvider, IEnumerable<IStyle>
    {
        private readonly Uri _baseUri;
        private IStyle? _loaded;
        private bool _isLoading;

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
        public FluentThemeMode Mode { get; set; }

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
                    _loaded = (IStyle)AvaloniaXamlLoader.Load(GetUri(), _baseUri);
                    _isLoading = false;
                }

                return _loaded;
            }
        }

        bool IResourceNode.HasResources => (Loaded as IResourceProvider)?.HasResources ?? false;

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

        private Uri GetUri() => Mode switch
        {
            FluentThemeMode.Dark => new Uri("avares://Avalonia.Themes.Fluent/FluentDark.xaml", UriKind.Absolute),
            _ => new Uri("avares://Avalonia.Themes.Fluent/FluentLight.xaml", UriKind.Absolute),
        };

        IEnumerator<IStyle> IEnumerable<IStyle>.GetEnumerator()
        {
            return ((IEnumerable<IStyle>)new[] { Loaded }).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new[] { Loaded }.GetEnumerator();
        }
    }
}
