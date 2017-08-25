// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;

namespace Avalonia.Styling
{
    /// <summary>
    /// A style that consists of a number of child styles.
    /// </summary>
    public class Styles : AvaloniaList<IStyle>, IStyle, ISetStyleParent
    {
        private IResourceNode _parent;
        private IResourceDictionary _resources;

        public Styles()
        {
            ResetBehavior = ResetBehavior.Remove;
            this.ForEachItem(
                x =>
                {
                    if (x.ResourceParent == null && x is ISetStyleParent setParent)
                    {
                        setParent.SetParent(this);
                        setParent.NotifyResourcesChanged(new ResourcesChangedEventArgs());
                    }

                    if (x.HasResources)
                    {
                        ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs());
                    }

                    x.ResourcesChanged += SubResourceChanged;
                },
                x =>
                {
                    if (x.ResourceParent == this && x is ISetStyleParent setParent)
                    {
                        setParent.SetParent(null);
                        setParent.NotifyResourcesChanged(new ResourcesChangedEventArgs());
                    }

                    if (x.HasResources)
                    {
                        ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs());
                    }

                    x.ResourcesChanged -= SubResourceChanged;
                },
                () => { });
        }

        /// <inheritdoc/>
        public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        /// <inheritdoc/>
        public bool HasResources => _resources?.Count > 0 || this.Any(x => x.HasResources);

        /// <summary>
        /// Gets or sets a dictionary of style resources.
        /// </summary>
        public IResourceDictionary Resources
        {
            get => _resources ?? (Resources = new ResourceDictionary());
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);

                var hadResources = false;

                if (_resources != null)
                {
                    hadResources = _resources.Count > 0;
                    _resources.ResourcesChanged -= ResourceDictionaryChanged;
                }

                _resources = value;
                _resources.ResourcesChanged += ResourceDictionaryChanged;

                if (hadResources || _resources.Count > 0)
                {
                    ((ISetStyleParent)this).NotifyResourcesChanged(new ResourcesChangedEventArgs());
                }
            }
        }

        /// <inheritdoc/>
        IResourceNode IResourceNode.ResourceParent => _parent;

        /// <summary>
        /// Attaches the style to a control if the style's selector matches.
        /// </summary>
        /// <param name="control">The control to attach to.</param>
        /// <param name="container">
        /// The control that contains this style. May be null.
        /// </param>
        public void Attach(IStyleable control, IStyleHost container)
        {
            foreach (IStyle style in this)
            {
                style.Attach(control, container);
            }
        }

        /// <inheritdoc/>
        public bool TryGetResource(string key, out object value)
        {
            if (_resources != null && _resources.TryGetValue(key, out value))
            {
                return true;
            }

            for (var i = Count - 1; i >= 0; --i)
            {
                if (this[i].TryGetResource(key, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <inheritdoc/>
        void ISetStyleParent.SetParent(IResourceNode parent)
        {
            if (_parent != null && parent != null)
            {
                throw new InvalidOperationException("The Style already has a parent.");
            }

            _parent = parent;
        }

        /// <inheritdoc/>
        void ISetStyleParent.NotifyResourcesChanged(ResourcesChangedEventArgs e)
        {
            ResourcesChanged?.Invoke(this, e);
        }

        private void ResourceDictionaryChanged(object sender, ResourcesChangedEventArgs e)
        {
            foreach (var child in this)
            {
                (child as ISetStyleParent)?.NotifyResourcesChanged(e);
            }

            ResourcesChanged?.Invoke(this, e);
        }

        private void SubResourceChanged(object sender, ResourcesChangedEventArgs e)
        {
            var foundSource = false;

            foreach (var child in this)
            {
                if (foundSource)
                {
                    (child as ISetStyleParent)?.NotifyResourcesChanged(e);
                }

                foundSource |= child == sender;
            }

            ResourcesChanged?.Invoke(this, e);
        }
    }
}
