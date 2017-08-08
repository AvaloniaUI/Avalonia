// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Styling;
using System;

namespace Avalonia.Markup.Xaml.Styling
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
        /// <param name="baseUri"></param>

        public StyleInclude(Uri baseUri)
        {
            _baseUri = baseUri;
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
        /// The resource if found, otherwise <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </returns>
        public object FindResource(string name)
        {
            return Loaded.FindResource(name);
        }
    }
}