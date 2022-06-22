using System;
using System.ComponentModel;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    /// <summary>
    /// Loads a resource dictionary from a specified URL.
    /// </summary>
    public class ResourceInclude : IResourceProvider
    {
        private Uri? _baseUri;
        private IResourceDictionary? _loaded;
        private bool _isLoading;

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
                    _loaded = (IResourceDictionary)AvaloniaXamlLoader.Load(Source, _baseUri);
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

        public ResourceInclude ProvideValue(IServiceProvider serviceProvider)
        {
            var tdc = (ITypeDescriptorContext)serviceProvider;
            _baseUri = tdc?.GetContextBaseUri();
            return this;
        }
    }
}
