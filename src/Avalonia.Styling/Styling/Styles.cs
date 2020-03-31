using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// A style that consists of a number of child styles.
    /// </summary>
    public class Styles : AvaloniaObject, IAvaloniaList<IStyle>, IStyle, ISetResourceParent
    {
        private readonly AvaloniaList<IStyle> _styles = new AvaloniaList<IStyle>();
        private IResourceNode? _parent;
        private IResourceDictionary? _resources;
        private Dictionary<Type, List<IStyle>?>? _cache;
        private bool _notifyingResourcesChanged;

        public Styles()
        {
            _styles.ResetBehavior = ResetBehavior.Remove;
            _styles.CollectionChanged += OnCollectionChanged;
        }

        public Styles(IResourceNode parent)
            : this()
        {
            _parent = parent;
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <inheritdoc/>
        public event EventHandler<ResourcesChangedEventArgs>? ResourcesChanged;

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
                value = value ?? throw new ArgumentNullException(nameof(Resources));

                var hadResources = false;

                if (_resources != null)
                {
                    hadResources = _resources.Count > 0;
                    _resources.ResourcesChanged -= NotifyResourcesChanged;
                }

                _resources = value;
                _resources.ResourcesChanged += NotifyResourcesChanged;

                if (hadResources || _resources.Count > 0)
                {
                    ((ISetResourceParent)this).ParentResourcesChanged(new ResourcesChangedEventArgs());
                }
            }
        }

        /// <inheritdoc/>
        IResourceNode? IResourceNode.ResourceParent => _parent;

        /// <inheritdoc/>
        bool ICollection<IStyle>.IsReadOnly => false;

        /// <inheritdoc/>
        IStyle IReadOnlyList<IStyle>.this[int index] => _styles[index];

        IReadOnlyList<IStyle> IStyle.Children => this;

        /// <inheritdoc/>
        public IStyle this[int index]
        {
            get => _styles[index];
            set => _styles[index] = value;
        }

        /// <inheritdoc/>
        public SelectorMatchResult TryAttach(IStyleable target, IStyleHost? host)
        {
            _cache ??= new Dictionary<Type, List<IStyle>?>();

            if (_cache.TryGetValue(target.StyleKey, out var cached))
            {
                if (cached is object)
                {
                    foreach (var style in cached)
                    {
                        style.TryAttach(target, host);
                    }

                    return SelectorMatchResult.AlwaysThisType;
                }
                else
                {
                    return SelectorMatchResult.NeverThisType;
                }
            }
            else
            {
                List<IStyle>? matches = null;

                foreach (var child in this)
                {
                    if (child.TryAttach(target, host) != SelectorMatchResult.NeverThisType)
                    {
                        matches ??= new List<IStyle>();
                        matches.Add(child);
                    }
                }

                _cache.Add(target.StyleKey, matches);

                return matches is null ?
                    SelectorMatchResult.NeverThisType :
                    SelectorMatchResult.AlwaysThisType;
            }
        }

        /// <inheritdoc/>
        public bool TryGetResource(object key, out object? value)
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

        public AvaloniaList<IStyle>.Enumerator GetEnumerator() => _styles.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator<IStyle> IEnumerable<IStyle>.GetEnumerator() => _styles.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => _styles.GetEnumerator();

        /// <inheritdoc/>
        void ISetResourceParent.SetParent(IResourceNode parent)
        {
            if (_parent != null && parent != null)
            {
                throw new InvalidOperationException("The Style already has a parent.");
            }

            _parent = parent;
        }

        /// <inheritdoc/>
        void ISetResourceParent.ParentResourcesChanged(ResourcesChangedEventArgs e)
        {
            NotifyResourcesChanged(e);
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            static IReadOnlyList<T> ToReadOnlyList<T>(IList list)
            {
                if (list is IReadOnlyList<T>)
                {
                    return (IReadOnlyList<T>)list;
                }
                else
                {
                    var result = new T[list.Count];
                    list.CopyTo(result, 0);
                    return result;
                }
            }

            void Add(IList items)
            {
                for (var i = 0; i < items.Count; ++i)
                {
                    var style = (IStyle)items[i];

                    if (style.ResourceParent == null && style is ISetResourceParent setParent)
                    {
                        setParent.SetParent(this);
                        setParent.ParentResourcesChanged(new ResourcesChangedEventArgs());
                    }

                    if (style.HasResources)
                    {
                        ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs());
                    }

                    style.ResourcesChanged += NotifyResourcesChanged;
                    _cache = null;
                }

                GetHost()?.StylesAdded(ToReadOnlyList<IStyle>(items));
            }

            void Remove(IList items)
            {
                for (var i = 0; i < items.Count; ++i)
                {
                    var style = (IStyle)items[i];

                    if (style.ResourceParent == this && style is ISetResourceParent setParent)
                    {
                        setParent.SetParent(null);
                        setParent.ParentResourcesChanged(new ResourcesChangedEventArgs());
                    }

                    if (style.HasResources)
                    {
                        ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs());
                    }

                    style.ResourcesChanged -= NotifyResourcesChanged;
                    _cache = null;
                }

                GetHost()?.StylesRemoved(ToReadOnlyList<IStyle>(items));
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Add(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Remove(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Remove(e.OldItems);
                    Add(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    throw new InvalidOperationException("Reset should not be called on Styles.");
            }

            CollectionChanged?.Invoke(this, e);
        }

        private IStyleHost? GetHost()
        {
            var node = _parent;

            while (node != null)
            {
                if (node is IStyleHost host)
                {
                    return host;
                }

                node = node.ResourceParent;
            }

            return null;
        }

        private void NotifyResourcesChanged(object sender, ResourcesChangedEventArgs e)
        {
            NotifyResourcesChanged(e);
        }

        private void NotifyResourcesChanged(ResourcesChangedEventArgs e)
        {
            if (_notifyingResourcesChanged)
            {
                return;
            }

            try
            {
                _notifyingResourcesChanged = true;
                foreach (var child in this)
                {
                    (child as ISetResourceParent)?.ParentResourcesChanged(e);
                }

                ResourcesChanged?.Invoke(this, e);
            }
            finally
            {
                _notifyingResourcesChanged = false;
            }
        }
    }
}
