using System;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Metadata;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Base class for <see cref="Style"/> and ControlTheme.
    /// </summary>
    public abstract class StyleBase : AvaloniaObject, IStyle, IResourceProvider
    {
        private IResourceHost? _owner;

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
            get => ResourcesCore ?? (Resources = new ResourceDictionary());
            set
            {
                value = value ?? throw new ArgumentNullException(nameof(value));

                var hadResources = ResourcesCore?.HasResources ?? false;

                ResourcesCore = value;

                if (Owner is object)
                {
                    ResourcesCore.AddOwner(Owner);

                    if (hadResources || ResourcesCore.HasResources)
                    {
                        Owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the style's setters.
        /// </summary>
        [Content]
        public IList<ISetter> Setters => SettersCore ??= new List<ISetter>();

        /// <summary>
        /// Gets the style's animations.
        /// </summary>
        public IList<IAnimation> Animations => AnimationsCore ??= new List<IAnimation>();

        /// <summary>
        /// Gets the style's child styles.
        /// </summary>
        /// <remarks>
        /// The default implementation is to return an empty list. This can be overridden in a
        /// derived class by overriding <see cref="GetChildrenCore"/>.
        /// </remarks>
        IReadOnlyList<IStyle> IStyle.Children => GetChildrenCore();

        /// <summary>
        /// Gets a value indicating whether the style has resources.
        /// </summary>
        /// <remarks>
        /// The implementation of this property can be overridden in a derived class by overriding
        /// <see cref="GetHasResourcesCore"/>.
        /// </remarks>
        bool IResourceNode.HasResources => GetHasResourcesCore();

        /// <summary>
        /// Gets the <see cref="Animations"/> for the control, without creating a collection
        /// if one does not already exist.
        /// </summary>
        protected List<IAnimation>? AnimationsCore { get; private set; }

        /// <summary>
        /// Gets the <see cref="Resources"/> for the control, without creating a resource
        /// dictionary if one does not already exist.
        /// </summary>
        protected IResourceDictionary? ResourcesCore { get; private set; }

        /// <summary>
        /// Gets the <see cref="Setters"/> for the control, without creating a collection
        /// if one does not already exist.
        /// </summary>
        protected List<ISetter>? SettersCore { get; private set; }

        public event EventHandler? OwnerChanged;

        public abstract SelectorMatchResult TryAttach(IStyleable target, object? host);

        public bool TryGetResource(object key, out object? result)
        {
            result = null;
            return ResourcesCore?.TryGetResource(key, out result) ?? false;
        }

        protected void Attach(IStyleable target)
        {
            if (SettersCore is object || AnimationsCore is object)
            {
                var instance = new StyleInstance(this, target, SettersCore, AnimationsCore);
                target.StyleApplied(instance);
                instance.Start();
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets the value for <see cref="IStyle.Children"/>.
        /// </summary>
        protected virtual IReadOnlyList<IStyle> GetChildrenCore() => Array.Empty<IStyle>();

        /// <summary>
        /// When overridden in a derived class, gets the value for <see cref="IResourceNode.HasResources"/>.
        /// </summary>
        protected virtual bool GetHasResourcesCore() => ResourcesCore?.Count > 0;

        void IResourceProvider.AddOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner != null)
            {
                throw new InvalidOperationException("The Style already has a parent.");
            }

            Owner = owner;
            ResourcesCore?.AddOwner(owner);
        }

        void IResourceProvider.RemoveOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner == owner)
            {
                Owner = null;
                ResourcesCore?.RemoveOwner(owner);
            }
        }
    }
}
