using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Metadata;
using Avalonia.PropertyStore;
using Avalonia.Styling.Activators;

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
        private StyleInstance? _sharedInstance;

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

        internal bool HasSettersOrAnimations => _setters?.Count > 0 || _animations?.Count > 0;

        public void Add(ISetter setter) => Setters.Add(setter);
        public void Add(IStyle style) => Children.Add(style);

        public event EventHandler? OwnerChanged;

        public abstract SelectorMatchResult TryAttach(IStyleable target, object? host);

        public bool TryGetResource(object key, out object? result)
        {
            result = null;
            return _resources?.TryGetResource(key, out result) ?? false;
        }

        internal ValueFrame Attach(IStyleable target, IStyleActivator? activator)
        {
            if (target is not AvaloniaObject ao)
                throw new InvalidOperationException("Styles can only be applied to AvaloniaObjects.");

            StyleInstance instance;

            if (_sharedInstance is not null)
            {
                instance = _sharedInstance;
            }
            else
            {
                var canShareInstance = activator is null;

                instance = new StyleInstance(this, activator);

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
                {
                    instance.MakeShared();
                    _sharedInstance = instance;
                }
            }

            ao.GetValueStore().AddFrame(instance);
            return instance;
        }

        internal SelectorMatchResult TryAttachChildren(IStyleable target, object? host)
        {
            if (_children is null || _children.Count == 0)
                return SelectorMatchResult.NeverThisType;
            _childCache ??= new StyleCache();
            return _childCache.TryAttach(_children, target, host);
        }

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
