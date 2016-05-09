// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Styling
{
    /// <summary>
    /// Includes a style from a URL.
    /// </summary>
    public class StyleInclude : IStyle
    {
        private Uri _baseUri;
        private IStyle _loaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleInclude"/> class.
        /// </summary>
        public StyleInclude()
        {
            // StyleInclude will usually be loaded from XAML and its URI can be relative to the
            // XAML file that its included in, so store the current XAML file's URI if any as
            // a base URI.
            _baseUri = PerspexXamlLoader.UriContext;
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
                    var loader = new PerspexXamlLoader();
                    _loaded = (IStyle)loader.Load(Source, _baseUri);
                }

                return _loaded;
            }
        }

        /// <inheritdoc/>
        public void Attach(IStyleable control, IStyleHost container)
        {
            if (Source != null)
            {
                Loaded.Attach(control, container);
            }
        }

        /// <summary>
        /// Tries to find a named resource within the style.
        /// </summary>
        /// <param name="name">The resource name.</param>
        /// <returns>
        /// The resource if found, otherwise <see cref="PerspexProperty.UnsetValue"/>.
        /// </returns>
        public object FindResource(string name)
        {
            return Loaded.FindResource(name);
        }
    }
}
