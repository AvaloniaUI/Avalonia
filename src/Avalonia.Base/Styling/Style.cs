using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a style.
    /// </summary>
    public class Style : AvaloniaObject, IStyle, IResourceProvider
    {
        private IResourceHost? _owner;
        private StyleChildren? _children;
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

        /// <summary>
        /// Gets the children of the style.
        /// </summary>
        public IList<IStyle> Children => _children ??= new(this);

        /// <summary>
        /// Gets the <see cref="StyledElement"/> or Application that hosts the style.
        /// </summary>
        public IResourceHost? Owner
        {
            get => _owner;
            private set
            {
                if (_owner != value)
                {
                    _owner = value;
                    OwnerChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets the parent style if this style is hosted in a <see cref="Style.Children"/> collection.
        /// </summary>
        public Style? Parent { get; private set; }

        /// <summary>
        /// Gets or sets a dictionary of style resources.
        /// </summary>
        public IResourceDictionary Resources
        {
            get => _resources ?? (Resources = new ResourceDictionary());
            set
            {
                value = value ?? throw new ArgumentNullException(nameof(value));

                var hadResources = _resources?.HasResources ?? false;

                _resources = value;

                if (Owner is object)
                {
                    _resources.AddOwner(Owner);

                    if (hadResources || _resources.HasResources)
                    {
                        Owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
                    }
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

        bool IResourceNode.HasResources => _resources?.Count > 0;
        IReadOnlyList<IStyle> IStyle.Children => (IReadOnlyList<IStyle>?)_children ?? Array.Empty<IStyle>();

        public event EventHandler? OwnerChanged;

        public SelectorMatchResult TryAttach(IStyleable target, IStyleHost? host)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));

            var match = Selector is object ? Selector.Match(target, Parent) :
                target == host ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;

            if (match.IsMatch && (_setters is object || _animations is object))
            {
                var instance = new StyleInstance(this, target, _setters, _animations, match.Activator);
                target.StyleApplied(instance);
                instance.Start();
            }

            var result = match.Result;

            if (_children is not null)
            {
                foreach (var child in _children)
                {
                    var childResult = child.TryAttach(target, host);
                    if (childResult > result)
                        result = childResult;
                }
            }

            return result;
        }

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

        void IResourceProvider.AddOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner != null)
            {
                throw new InvalidOperationException("The Style already has a parent.");
            }

            Owner = owner;
            _resources?.AddOwner(owner);
        }

        void IResourceProvider.RemoveOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner == owner)
            {
                Owner = null;
                _resources?.RemoveOwner(owner);
            }
        }

        internal void SetParent(Style? parent)
        {
            if (parent?.Selector is not null)
            {
                if (Selector is null)
                    throw new InvalidOperationException("Nested styles must have a selector.");
                // TODO: Validate that selector contains & in the right place.
            }

            Parent = parent;
        }
    }
}
