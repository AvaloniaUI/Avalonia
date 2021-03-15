using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Themes.Fluent
{
    public class FluentStyleInclude : IStyle, IResourceProvider
    {
        private readonly Uri _baseUri;
        private IResourceHost? _owner;
        private IStyle[]? _loaded;
        private bool _isLoading;

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentStyleInclude"/> class.
        /// </summary>
        /// <param name="baseUri">The base URL for the XAML context.</param>
        public FluentStyleInclude(Uri baseUri)
        {
            _baseUri = baseUri;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentStyleInclude"/> class.
        /// </summary>
        /// <param name="serviceProvider">The XAML service provider.</param>
        public FluentStyleInclude(IServiceProvider serviceProvider)
        {
            _baseUri = ((IUriContext)serviceProvider.GetService(typeof(IUriContext))).BaseUri;
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
                    var loaded = (IStyle)AvaloniaXamlLoader.Load(Source, _baseUri);
                    _loaded = new[] { loaded };
                    _isLoading = false;
                    if (_owner is object)
                        (loaded as IResourceProvider)?.AddOwner(_owner);
                }

                return _loaded?[0]!;
            }
        }

        public Type? Filter { get; set; }

        bool IResourceNode.HasResources => (Loaded as IResourceProvider)?.HasResources ?? false;

        IReadOnlyList<IStyle> IStyle.Children => _loaded ?? Array.Empty<IStyle>();

        public event EventHandler OwnerChanged
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

        public SelectorMatchResult TryAttach(IStyleable target, IStyleHost? host)
        {
            if (_loaded is null && (Filter is null || target.GetType() != Filter))
                return SelectorMatchResult.NeverThisType;
            return Loaded.TryAttach(target, host);
        }

        public bool TryGetResource(object key, out object? value)
        {
            if (Filter is object)
            {
                value = null;
                return false;
            }

            if (!_isLoading && Loaded is IResourceProvider p)
            {
                return p.TryGetResource(key, out value);
            }

            value = null;
            return false;
        }

        void IResourceProvider.AddOwner(IResourceHost owner)
        {
            _owner = owner;
            if (_loaded is object)
                (_loaded[0] as IResourceProvider)?.AddOwner(owner);
        }

        void IResourceProvider.RemoveOwner(IResourceHost owner)
        {
            if (owner == _owner)
            {
                _owner = null;
                if (_loaded is object)
                    (_loaded[0] as IResourceProvider)?.RemoveOwner(owner);
            }
        }
    }
}
