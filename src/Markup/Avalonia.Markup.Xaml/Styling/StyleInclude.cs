// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Styling;
using System;
using Avalonia.Controls;
using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.Styling
{
    /// <summary>
    /// Includes a style from a URL.
    /// </summary>
    public class StyleInclude : IStyle, ISetResourceParent
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
                    (_loaded as ISetResourceParent)?.SetParent(this);
                }

                return _loaded;
            }
        }

        /// <inheritdoc/>
        bool IResourceProvider.HasResources => Loaded.HasResources;

        /// <inheritdoc/>
        IResourceNode IResourceNode.ResourceParent => _parent;

        /// <inheritdoc/>
        public SelectorMatchResult TryAttach(IStyleable target, IStyleHost host) => Loaded.TryAttach(target, host);

        /// <inheritdoc/>
        public bool TryGetResource(object key, out object value) => Loaded.TryGetResource(key, out value);

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
