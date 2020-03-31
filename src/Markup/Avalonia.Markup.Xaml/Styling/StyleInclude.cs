using Avalonia.Styling;
using System;
using Avalonia.Controls;
using System.Collections.Generic;

#nullable enable

namespace Avalonia.Markup.Xaml.Styling
{
    /// <summary>
    /// Includes a style from a URL.
    /// </summary>
    public class StyleInclude : IStyle, ISetResourceParent
    {
        private Uri _baseUri;
        private IStyle[]? _loaded;
        private IResourceNode? _parent;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleInclude"/> class.
        /// </summary>
        /// <param name="baseUri"></param>
        public StyleInclude(Uri baseUri)
        {
            _baseUri = baseUri;
        }

        public StyleInclude(IServiceProvider serviceProvider)
        {
            _baseUri = serviceProvider.GetContextBaseUri();
        }

        /// <inheritdoc/>
        public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged
        {
            add {}
            remove {}
        }

        /// <summary>
        /// Gets or sets the source URL.
        /// </summary>
        public Uri? Source { get; set; }

        /// <summary>
        /// Gets the loaded style.
        /// </summary>
        public IStyle Loaded
        {
            get
            {
                if (_loaded == null)
                {
                    var loader = new AvaloniaXamlLoader();
                    var loaded = (IStyle)loader.Load(Source, _baseUri);
                    (loaded as ISetResourceParent)?.SetParent(this);
                    _loaded = new[] { loaded };
                }

                return _loaded?[0]!;
            }
        }

        /// <inheritdoc/>
        bool IResourceProvider.HasResources => Loaded.HasResources;

        /// <inheritdoc/>
        IResourceNode? IResourceNode.ResourceParent => _parent;

        IReadOnlyList<IStyle> IStyle.Children => _loaded ?? Array.Empty<IStyle>();

        /// <inheritdoc/>
        public SelectorMatchResult TryAttach(IStyleable target, IStyleHost? host) => Loaded.TryAttach(target, host);

        /// <inheritdoc/>
        public bool TryGetResource(object key, out object? value) => Loaded.TryGetResource(key, out value);

        /// <inheritdoc/>
        void ISetResourceParent.ParentResourcesChanged(ResourcesChangedEventArgs e)
        {
            (Loaded as ISetResourceParent)?.ParentResourcesChanged(e);
        }

        /// <inheritdoc/>
        void ISetResourceParent.SetParent(IResourceNode parent)
        {
            if (_parent != null && parent != null)
            {
                throw new InvalidOperationException("The Style already has a parent.");
            }

            _parent = parent;
        }
    }
}
