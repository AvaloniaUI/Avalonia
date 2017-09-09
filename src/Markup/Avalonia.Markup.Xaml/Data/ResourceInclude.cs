using System;
using System.ComponentModel;
using Avalonia.Controls;
using Portable.Xaml.ComponentModel;
using Portable.Xaml.Markup;

namespace Avalonia.Markup.Xaml.Data
{
    /// <summary>
    /// Loads a resource dictionary from a specified URL.
    /// </summary>
    public class ResourceInclude : MarkupExtension, IResourceProvider
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
        bool IResourceProvider.TryGetResource(string key, out object value)
        {
            return Loaded.TryGetResource(key, out value);
        }

        /// <inhertidoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var tdc = (ITypeDescriptorContext)serviceProvider;
            _baseUri = tdc?.GetBaseUri();
            return this;
        }
    }
}
