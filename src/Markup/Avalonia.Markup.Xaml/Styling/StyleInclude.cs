using Avalonia.Styling;
using System;
using Avalonia.Controls;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Markup.Xaml.Styling
{
    /// <summary>
    /// Includes a style from a URL.
    /// </summary>
    /// <remarks>
    /// If used from the XAML code, it is replaced with direct style reference.
    /// When used in runtime, this type might be unsafe with trimming and AOT.
    /// </remarks>
    [RequiresUnreferencedCode(TrimmingMessages.StyleResourceIncludeRequiresUnreferenceCodeMessage)]
    public class StyleInclude : IStyle, IResourceProvider
    {
        private readonly IServiceProvider? _serviceProvider;
        private readonly Uri? _baseUri;
        private IStyle[]? _loaded;
        private bool _isLoading;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleInclude"/> class.
        /// </summary>
        /// <param name="baseUri">The base URL for the XAML context.</param>
        public StyleInclude(Uri? baseUri)
        {
            _baseUri = baseUri;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StyleInclude"/> class.
        /// </summary>
        /// <param name="serviceProvider">The XAML service provider.</param>
        public StyleInclude(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _baseUri = serviceProvider.GetContextBaseUri();
        }

        public IResourceHost? Owner => (Loaded as IResourceProvider)?.Owner;

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
                    _isLoading = true;
                    var source = Source ?? throw new InvalidOperationException("StyleInclude.Source must be set.");
                    var loaded = (IStyle)AvaloniaXamlLoader.Load(_serviceProvider, source, _baseUri);
                    _loaded = new[] { loaded };
                    _isLoading = false;
                }

                return _loaded[0];
            }
        }

        bool IResourceNode.HasResources => Loaded.HasResources;

        IReadOnlyList<IStyle> IStyle.Children => _loaded ?? Array.Empty<IStyle>();

        public event EventHandler? OwnerChanged
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

        public bool TryGetResource(object key, ThemeVariant? theme, out object? value)
        {
            if (!_isLoading)
            {
                return Loaded.TryGetResource(key, theme, out value);
            }

            value = null;
            return false;
        }

        void IResourceProvider.AddOwner(IResourceHost owner) => (Loaded as IResourceProvider)?.AddOwner(owner);
        void IResourceProvider.RemoveOwner(IResourceHost owner) => (Loaded as IResourceProvider)?.RemoveOwner(owner);
    }
}
