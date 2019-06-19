// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Styling;
using System;
using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.Styling
{
    /// <summary>
    /// Includes a style from a URL.
    /// </summary>
    public class StyleInclude : IStyle, ISetStyleParent
    {
        private Uri _baseUri;
        private IStyle _loaded;
        private IResourceNode _parent;

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
        public Uri Source { get; set; }

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
                    _loaded = (IStyle)loader.Load(Source, _baseUri);
                    (_loaded as ISetStyleParent)?.SetParent(this);
                }

                return _loaded;
            }
        }

        /// <inheritdoc/>
        bool IResourceProvider.HasResources => Loaded.HasResources;

        /// <inheritdoc/>
        IResourceNode IResourceNode.ResourceParent => _parent;

        /// <inheritdoc/>
        public bool Attach(IStyleable control, IStyleHost container)
        {
            if (Source != null)
            {
                return Loaded.Attach(control, container);
            }

            return false;
        }

        public void Detach()
        {
            if (Source != null)
            {
                Loaded.Detach();
            }
        }

        /// <inheritdoc/>
        public bool TryGetResource(object key, out object value) => Loaded.TryGetResource(key, out value);

        /// <inheritdoc/>
        void ISetStyleParent.NotifyResourcesChanged(ResourcesChangedEventArgs e)
        {
            (Loaded as ISetStyleParent)?.NotifyResourcesChanged(e);
        }

        /// <inheritdoc/>
        void ISetStyleParent.SetParent(IResourceNode parent)
        {
            if (_parent != null && parent != null)
            {
                throw new InvalidOperationException("The Style already has a parent.");
            }

            _parent = parent;
        }
    }
}
