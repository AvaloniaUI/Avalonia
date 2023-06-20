using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.PropertyStore;

namespace Avalonia.Styling
{
    /// <summary>
    /// A style that consists of a number of child styles.
    /// </summary>
    public class Styles : AvaloniaObject,
        IAvaloniaList<IStyle>,
        IStyle,
        IResourceProvider
    {
        private readonly AvaloniaList<IStyle> _styles = new();
        private IResourceHost? _owner;
        private IResourceDictionary? _resources;

        public Styles()
        {
            _styles.ResetBehavior = ResetBehavior.Remove;
            _styles.CollectionChanged += OnCollectionChanged;
            _styles.Validate = i =>
            {
                if (i is ControlTheme)
                    throw new InvalidOperationException("ControlThemes cannot be added to a Styles collection.");
            };
        }

        public Styles(IResourceHost owner)
            : this()
        {
            Owner = owner;
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event EventHandler? OwnerChanged;

        public int Count => _styles.Count;

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
                value = value ?? throw new ArgumentNullException(nameof(Resources));

                var currentOwner = Owner;

                if (currentOwner is not null)
                {
                    _resources?.RemoveOwner(currentOwner);
                }

                _resources = value;

                if (currentOwner is not null)
                {
                    _resources.AddOwner(currentOwner);
                }
            }
        }

        bool ICollection<IStyle>.IsReadOnly => false;

        bool IResourceNode.HasResources
        {
            get
            {
                if (_resources?.Count > 0)
                {
                    return true;
                }

                foreach (var i in this)
                {
                    if (i is IResourceProvider { HasResources: true })
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        IStyle IReadOnlyList<IStyle>.this[int index] => _styles[index];

        IReadOnlyList<IStyle> IStyle.Children => this;

        public IStyle this[int index]
        {
            get => _styles[index];
            set => _styles[index] = value;
        }

        /// <inheritdoc/>
        public bool TryGetResource(object key, ThemeVariant? theme, out object? value)
        {
            if (_resources != null && _resources.TryGetResource(key, theme, out value))
            {
                return true;
            }

            for (var i = Count - 1; i >= 0; --i)
            {
                if (this[i].TryGetResource(key, theme, out value))
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
        void IResourceProvider.AddOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner is not null)
            {
                throw new InvalidOperationException("The Styles already has a owner.");
            }

            Owner = owner;
            _resources?.AddOwner(owner);

            foreach (var child in this)
            {
                if (child is IResourceProvider r)
                {
                    r.AddOwner(owner);
                }
            }
        }

        /// <inheritdoc/>
        void IResourceProvider.RemoveOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner == owner)
            {
                Owner = null;
                _resources?.RemoveOwner(owner);

                foreach (var child in this)
                {
                    if (child is IResourceProvider r)
                    {
                        r.RemoveOwner(owner);
                    }
                }
            }
        }

        internal SelectorMatchResult TryAttach(StyledElement target, object? host)
        {
            var result = SelectorMatchResult.NeverThisType;

            foreach (var s in this)
            {
                if (s is not Style style)
                    continue;
                var r = style.TryAttach(target, host, FrameType.Style);
                if (r > result)
                    result = r;
            }

            return result;
        }

        private static IReadOnlyList<T> ToReadOnlyList<T>(ICollection list)
        {
            if (list is IReadOnlyList<T> readOnlyList)
            {
                return readOnlyList;
            }

            var result = new T[list.Count];
            list.CopyTo(result, 0);
            return result;
        }

        private static void InternalAdd(IList items, IResourceHost? owner)
        {
            if (owner is not null)
            {
                for (var i = 0; i < items.Count; ++i)
                {
                    if (items[i] is IResourceProvider provider)
                    {
                        provider.AddOwner(owner);
                    }
                }

                (owner as IStyleHost)?.StylesAdded(ToReadOnlyList<IStyle>(items));
            }
        }

        private static void InternalRemove(IList items, IResourceHost? owner)
        {
            if (owner is not null)
            {
                for (var i = 0; i < items.Count; ++i)
                {
                    if (items[i] is IResourceProvider provider)
                    {
                        provider.RemoveOwner(owner);
                    }
                }

                (owner as IStyleHost)?.StylesRemoved(ToReadOnlyList<IStyle>(items));
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                throw new InvalidOperationException("Reset should not be called on Styles.");
            }

            var currentOwner = Owner;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    InternalAdd(e.NewItems!, currentOwner);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    InternalRemove(e.OldItems!, currentOwner);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    InternalRemove(e.OldItems!, currentOwner);
                    InternalAdd(e.NewItems!, currentOwner);
                    break;
            }

            CollectionChanged?.Invoke(this, e);
        }
    }
}
