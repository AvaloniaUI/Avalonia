using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace Avalonia.Styling
{
    /// <summary>
    /// Base class for <see cref="Style"/> and <see cref="ControlTheme"/>.
    /// </summary>
    public abstract class StyleBase : AvaloniaObject, IStyle, IResourceProvider
    {
        private IResourceHost? _owner;
        private StyleChildren? _children;
        private IResourceDictionary? _resources;
        private List<ISetter>? _setters;
        private List<IAnimation>? _animations;
        private StyleCache? _childCache;

        public IList<IStyle> Children => _children ??= new(this);

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

        public IStyle? Parent { get; private set; }

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

        public IList<ISetter> Setters => _setters ??= new List<ISetter>();
        public IList<IAnimation> Animations => _animations ??= new List<IAnimation>();

        bool IResourceNode.HasResources => _resources?.Count > 0;
        IReadOnlyList<IStyle> IStyle.Children => (IReadOnlyList<IStyle>?)_children ?? Array.Empty<IStyle>();

        public void Add(ISetter setter) => Setters.Add(setter);
        public void Add(IStyle style) => Children.Add(style);

        public event EventHandler? OwnerChanged;

        public SelectorMatchResult TryAttach(IStyleable target, IStyleHost? host)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));

            var result = SelectorMatchResult.NeverThisType;

            if (_setters?.Count > 0 || _animations?.Count > 0)
            {
                var match = Matches(target, host);

                if (match.IsMatch)
                {
                    var instance = new StyleInstance(this, target, _setters, _animations, match.Activator);
                    target.StyleApplied(instance);
                    instance.Start();
                }

                result = match.Result;
            }

            if (_children is not null)
            {
                _childCache ??= new StyleCache();
                var childResult = _childCache.TryAttach(_children, target, host);
                if (childResult > result)
                    result = childResult;
            }

            return result;
        }

        public bool TryGetResource(object key, out object? result)
        {
            result = null;
            return _resources?.TryGetResource(key, out result) ?? false;
        }

        protected abstract SelectorMatch Matches(IStyleable target, IStyleHost? host);

        internal virtual void SetParent(StyleBase? parent) => Parent = parent;

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
