using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.PropertyStore;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a style.
    /// </summary>
    public class Style : AvaloniaObject, IStyle, IResourceProvider
    {
        private IResourceHost? _owner;
        private IResourceDictionary? _resources;
        private List<ISetter>? _setters;
        private List<IAnimation>? _animations;
        private StyleInstance? _sharedInstance;

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

        public event EventHandler? OwnerChanged;

        internal SelectorMatchResult Instance(IStyleable target, IStyleHost? host, out IValueFrame? frame)
        {
            var match = Selector is object ?
                Selector.Match(target, true) :
                target == host ? SelectorMatch.AlwaysThisInstance : SelectorMatch.NeverThisInstance;

            if (match.IsMatch != true)
            {
                frame = null;
                return match.Result;
            }

            if (_sharedInstance is object)
            {
                frame = _sharedInstance;
                return SelectorMatchResult.AlwaysThisType;
            }

            var instance = new StyleInstance(this, match.Activator);
            var canShareInstance = match.Activator is null;

            if (_setters is object)
            {
                foreach (var setter in _setters)
                {
                    var setterInstance = setter.Instance(instance, target);
                    instance.Add(setterInstance);
                    canShareInstance &= setterInstance == setter;
                }
            }

            if (canShareInstance)
                _sharedInstance = instance;

            frame = instance;
            return match.Result;
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
    }
}
