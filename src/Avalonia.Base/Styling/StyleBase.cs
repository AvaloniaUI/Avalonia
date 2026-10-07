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
    public abstract class StyleBase : AvaloniaObject, IStyle, IResourceProvider, IAddChild
    {
        private IResourceHost? _owner;
        private StyleChildren? _children;
        private IResourceDictionary? _resources;
        private List<SetterBase>? _setters;
        private List<IAnimation>? _animations;
        // Shared instances are cached per FrameType: the same ControlTheme can
        // be attached to a control both as its own Theme and as a
        // TemplatedParentTheme, and a single StyleInstance object must never be
        // inserted twice into the same ValueStore (nor shared across different
        // frame types). The vast majority of styles only ever have a single
        // shared instance, so we keep a plain field for that case and only
        // allocate an array indexed by FrameType once a second frame type is
        // shared. (InlineDictionary requires a class type key, so it cannot be
        // used here.)
        private StyleInstance? _sharedInstance;
        private FrameType _sharedInstanceType;
        private StyleInstance?[]? _sharedInstances;

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
                        Owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());
                    }
                }
            }
        }

        public IList<SetterBase> Setters => _setters ??= new();
        public IList<IAnimation> Animations => _animations ??= new List<IAnimation>();

        bool IResourceNode.HasResources => _resources?.Count > 0;
        IReadOnlyList<IStyle> IStyle.Children => (IReadOnlyList<IStyle>?)_children ?? Array.Empty<IStyle>();

        internal bool HasChildren => _children?.Count > 0;
        internal bool HasSettersOrAnimations => _setters?.Count > 0 || _animations?.Count > 0;

        public void Add(SetterBase setter) => Setters.Add(setter);
        public void Add(IStyle style) => Children.Add(style);

        void IAddChild.AddChild(object child)
        {
            switch (child)
            {
                case SetterBase setter:
                    Setters.Add(setter);
                    break;
                case IStyle style:
                    Children.Add(style);
                    break;
                default:
                    throw new InvalidOperationException($"Cannot add {child.GetType()} to a style.");
            }
        }

        public event EventHandler? OwnerChanged;

        public bool TryGetResource(object key, ThemeVariant? themeVariant, out object? result)
        {
            if (_resources is not null && _resources.TryGetResource(key, themeVariant, out result))
                return true;

            if (_children is not null)
            {
                for (var i = 0; i < _children.Count; ++i)
                {
                    if (_children[i].TryGetResource(key, themeVariant, out result))
                        return true;
                }
            }

            result= null;
            return false;
        }

        internal ValueFrame Attach(
            StyledElement target,
            IStyleActivator? activator,
            FrameType type,
            bool canShareInstance)
        {
            if (target is not AvaloniaObject ao)
                throw new InvalidOperationException("Styles can only be applied to AvaloniaObjects.");

            StyleInstance instance;

            var shared = GetSharedInstance(type);

            if (shared is not null && canShareInstance)
            {
                instance = shared;
            }
            else
            {
                canShareInstance &= activator is null;

                instance = new StyleInstance(this, activator, type);

                if (_setters is not null)
                {
                    foreach (var setter in _setters)
                    {
                        var setterInstance = setter.Instance(instance, target);
                        instance.Add(setterInstance);
                        canShareInstance &= setterInstance == setter;
                    }
                }

                if (_animations is not null)
                    instance.Add(_animations);

                if (canShareInstance)
                {
                    instance.MakeShared();
                    SetSharedInstance(type, instance);
                }
            }

            ao.GetValueStore().AddFrame(instance);
            instance.ApplyAnimations(ao);
            return instance;
        }

        private StyleInstance? GetSharedInstance(FrameType type)
        {
            if (_sharedInstances is not null)
            {
                return _sharedInstances[(int)type];
            }

            return _sharedInstance is not null && _sharedInstanceType == type ? _sharedInstance : null;
        }

        private void SetSharedInstance(FrameType type, StyleInstance instance)
        {
            if (_sharedInstances is not null)
            {
                _sharedInstances[(int)type] = instance;
            }
            else if (_sharedInstance is null)
            {
                _sharedInstance = instance;
                _sharedInstanceType = type;
            }
            else
            {
                var arr = new StyleInstance?[3];
                arr[(int)_sharedInstanceType] = _sharedInstance;
                arr[(int)type] = instance;
                _sharedInstances = arr;
                _sharedInstance = null;
            }
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
