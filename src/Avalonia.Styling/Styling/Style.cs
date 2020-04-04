using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Metadata;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a style.
    /// </summary>
    public class Style : AvaloniaObject, IStyle, ISetResourceParent
    {
        private IResourceNode? _parent;
        private IResourceDictionary? _resources;
        private List<ISetter>? _setters;
        private List<IAnimation>? _animations;

        /// <summary>
        /// Initializes a new instance of the <see cref="Style"/> class.
        /// </summary>
        public Style()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Style"/> class.
        /// </summary>
        /// <param name="selector">The style selector.</param>
        public Style(Func<Selector?, Selector> selector)
        {
            Selector = selector(null);
        }

        /// <inheritdoc/>
        public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;

        /// <summary>
        /// Gets or sets a dictionary of style resources.
        /// </summary>
        public IResourceDictionary Resources
        {
            get => _resources ?? (Resources = new ResourceDictionary());
            set
            {
                value = value ?? throw new ArgumentNullException(nameof(value));

                var hadResources = false;

                if (_resources != null)
                {
                    hadResources = _resources.HasResources;
                    _resources.ResourcesChanged -= ResourceDictionaryChanged;
                }

                _resources = value;
                _resources.ResourcesChanged += ResourceDictionaryChanged;

                if (hadResources || _resources.HasResources)
                {
                    ((ISetResourceParent)this).ParentResourcesChanged(new ResourcesChangedEventArgs());
                }
            }
        }

        /// <summary>
        /// Gets or sets the style's selector.
        /// </summary>
        public Selector? Selector { get; set; }

        /// <summary>
        /// Gets the style's setters.
        /// </summary>
        [Content]
        public IList<ISetter> Setters => _setters ??= new List<ISetter>();

        /// <summary>
        /// Gets the style's animations.
        /// </summary>
        public IList<IAnimation> Animations => _animations ??= new List<IAnimation>();

        /// <inheritdoc/>
        IResourceNode? IResourceNode.ResourceParent => _parent;

        /// <inheritdoc/>
        bool IResourceProvider.HasResources => _resources?.Count > 0;

        IReadOnlyList<IStyle> IStyle.Children => Array.Empty<IStyle>();

        /// <inheritdoc/>
        public SelectorMatchResult TryAttach(IStyleable target, IStyleHost? host)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));

            var match = Selector is object ? Selector.Match(target) :
                target == host ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;

            if (match.IsMatch && (_setters is object || _animations is object))
            {
                var instance = new StyleInstance(this, target, _setters, _animations, match.Activator);
                target.StyleApplied(instance);
                instance.Start();
            }

            return match.Result;
        }

        /// <inheritdoc/>
        public bool TryGetResource(object key, out object? result)
        {
            result = null;
            return _resources?.TryGetResource(key, out result) ?? false;
        }

        /// <summary>
        /// Returns a string representation of the style.
        /// </summary>
        /// <returns>A string representation of the style.</returns>
        public override string ToString()
        {
            if (Selector != null)
            {
                return "Style: " + Selector.ToString();
            }
            else
            {
                return "Style";
            }
        }

        /// <inheritdoc/>
        void ISetResourceParent.ParentResourcesChanged(ResourcesChangedEventArgs e)
        {
            ResourcesChanged?.Invoke(this, e);
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

        private void ResourceDictionaryChanged(object sender, ResourcesChangedEventArgs e)
        {
            ResourcesChanged?.Invoke(this, e);
        }
    }
}
