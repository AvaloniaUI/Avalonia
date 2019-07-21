using System;
using System.ComponentModel;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    /// <summary>
    /// Loads a resource dictionary from a specified URL.
    /// </summary>
    public class ResourceInclude :IResourceProvider
    {
        private Uri _baseUri;
        private IResourceDictionary _loaded;

        public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        /// <summary>
        /// Gets the loaded resource dictionary.
        /// </summary>
        public IResourceDictionary Loaded
        {
            get
            {
                if (_loaded == null)
                {
                    var loader = new AvaloniaXamlLoader();
                    _loaded = (IResourceDictionary)loader.Load(Source, _baseUri);

                    if (_loaded.HasResources)
                    {
                        ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs());
                    }
                }

                return _loaded;
            }
        }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        public Uri Source { get; set; }

        /// <inhertidoc/>
        bool IResourceProvider.HasResources => Loaded.HasResources;

        /// <inhertidoc/>
        bool IResourceProvider.TryGetResource(object key, out object value)
        {
            return Loaded.TryGetResource(key, out value);
        }

        public ResourceInclude ProvideValue(IServiceProvider serviceProvider)
        {
            var tdc = (ITypeDescriptorContext)serviceProvider;
            _baseUri = tdc?.GetContextBaseUri();
            return this;
        }
    }
}
