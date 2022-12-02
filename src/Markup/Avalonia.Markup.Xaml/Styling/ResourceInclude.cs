using System;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Markup.Xaml.Styling
{
    /// <summary>
    /// Loads a resource dictionary from a specified URL.
    /// </summary>
    public class ResourceInclude : IResourceProvider
    {
        private readonly Uri? _baseUri;
        private IResourceDictionary? _loaded;
        private bool _isLoading;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceInclude"/> class.
        /// </summary>
        /// <param name="baseUri">The base URL for the XAML context.</param>
        public ResourceInclude(Uri? baseUri)
        {
            _baseUri = baseUri;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceInclude"/> class.
        /// </summary>
        /// <param name="serviceProvider">The XAML service provider.</param>
        public ResourceInclude(IServiceProvider serviceProvider)
        {
            _baseUri = serviceProvider.GetContextBaseUri();
        }

        /// <summary>
        /// Gets the loaded resource dictionary.
        /// </summary>
        public IResourceDictionary Loaded
        {
            get
            {
                if (_loaded == null)
                {
                    _isLoading = true;
                    var source = Source ?? throw new InvalidOperationException("ResourceInclude.Source must be set.");
                    _loaded = (IResourceDictionary)AvaloniaXamlLoader.Load(source, _baseUri);
                    _isLoading = false;
                }

                return _loaded;
            }
        }

        public IResourceHost? Owner => Loaded.Owner;

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        public Uri? Source { get; set; }

        bool IResourceNode.HasResources => Loaded.HasResources;

        public event EventHandler? OwnerChanged
        {
            add => Loaded.OwnerChanged += value;
            remove => Loaded.OwnerChanged -= value;
        }

        bool IResourceNode.TryGetResource(object key, out object? value)
        {
            if (!_isLoading)
            {
                return Loaded.TryGetResource(key, out value);
            }

            value = null;
            return false;
        }

        void IResourceProvider.AddOwner(IResourceHost owner) => Loaded.AddOwner(owner);
        void IResourceProvider.RemoveOwner(IResourceHost owner) => Loaded.RemoveOwner(owner);
    }
}
