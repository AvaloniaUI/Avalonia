// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;

namespace Avalonia.Styling
{
    /// <summary>
    /// A style that consists of a number of child styles.
    /// </summary>
    public class Styles : AvaloniaObject, IAvaloniaList<IStyle>, IStyle, ISetStyleParent
    {
        private IResourceNode _parent;
        private IResourceDictionary _resources;
        private AvaloniaList<IStyle> _styles = new AvaloniaList<IStyle>();
        private Dictionary<Type, List<IStyle>> _cache;

        public Styles()
        {
            _styles.ResetBehavior = ResetBehavior.Remove;
            _styles.ForEachItem(
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
                    _cache = null;
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
                    _cache = null;
                },
                () => { });
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => _styles.CollectionChanged += value;
            remove => _styles.CollectionChanged -= value;
        }

        /// <inheritdoc/>
        public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        /// <inheritdoc/>
        public int Count => _styles.Count;

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

        /// <inheritdoc/>
        bool ICollection<IStyle>.IsReadOnly => false;

        /// <inheritdoc/>
        IStyle IReadOnlyList<IStyle>.this[int index] => _styles[index];

        /// <inheritdoc/>
        public IStyle this[int index]
        {
            get => _styles[index];
            set => _styles[index] = value;
        }

        /// <summary>
        /// Attaches the style to a control if the style's selector matches.
        /// </summary>
        /// <param name="control">The control to attach to.</param>
        /// <param name="container">
        /// The control that contains this style. May be null.
        /// </param>
        public bool Attach(IStyleable control, IStyleHost container)
        {
            if (_cache == null)
            {
                _cache = new Dictionary<Type, List<IStyle>>();
            }

            if (_cache.TryGetValue(control.StyleKey, out var cached))
            {
                if (cached != null)
                {
                    foreach (var style in cached)
                    {
                        style.Attach(control, container);
                    }

                    return true;
                }

                return false;
            }
            else
            {
                List<IStyle> result = null;

                foreach (var style in this)
                {
                    if (style.Attach(control, container))
                    {
                        if (result == null)
                        {
                            result = new List<IStyle>();
                        }

                        result.Add(style);
                    }
                }

                _cache.Add(control.StyleKey, result);
                return result != null;
            }
        }

        public void Detach()
        {
            foreach (IStyle style in this)
            {
                style.Detach();
            }
        }

        /// <inheritdoc/>
        public bool TryGetResource(object key, out object value)
        {
            if (_resources != null && _resources.TryGetResource(key, out value))
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
        public void AddRange(IEnumerable<IStyle> items) => _styles.AddRange(items);

        /// <inheritdoc/>
        public void InsertRange(int index, IEnumerable<IStyle> items) => _styles.InsertRange(index, items);

        /// <inheritdoc/>
        public void Move(int oldIndex, int newIndex) => _styles.Move(oldIndex, newIndex);

        /// <inheritdoc/>
        public void MoveRange(int oldIndex, int count, int newIndex) => _styles.MoveRange(oldIndex, count, newIndex);

        /// <inheritdoc/>
        public void RemoveAll(IEnumerable<IStyle> items) => _styles.RemoveAll(items);

        /// <inheritdoc/>
        public void RemoveRange(int index, int count) => _styles.RemoveRange(index, count);

        /// <inheritdoc/>
        public int IndexOf(IStyle item) => _styles.IndexOf(item);

        /// <inheritdoc/>
        public void Insert(int index, IStyle item) => _styles.Insert(index, item);

        /// <inheritdoc/>
        public void RemoveAt(int index) => _styles.RemoveAt(index);

        /// <inheritdoc/>
        public void Add(IStyle item) => _styles.Add(item);

        /// <inheritdoc/>
        public void Clear() => _styles.Clear();

        /// <inheritdoc/>
        public bool Contains(IStyle item) => _styles.Contains(item);

        /// <inheritdoc/>
        public void CopyTo(IStyle[] array, int arrayIndex) => _styles.CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public bool Remove(IStyle item) => _styles.Remove(item);

        /// <inheritdoc/>
        public IEnumerator<IStyle> GetEnumerator() => _styles.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => _styles.GetEnumerator();

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
