using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public class ResourceDictionary : IResourceDictionary
    {
        private Dictionary<object, object?>? _inner;
        private IResourceHost? _owner;
        private AvaloniaList<IResourceProvider>? _mergedDictionaries;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDictionary"/> class.
        /// </summary>
        public ResourceDictionary() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDictionary"/> class.
        /// </summary>
        public ResourceDictionary(IResourceHost owner) => Owner = owner;

        public int Count => _inner?.Count ?? 0;

        public object? this[object key]
        {
            get => _inner?[key];
            set
            {
                Inner[key] = value;
                Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
            }
        }

        public ICollection<object> Keys => (ICollection<object>?)_inner?.Keys ?? Array.Empty<object>();
        public ICollection<object?> Values => (ICollection<object?>?)_inner?.Values ?? Array.Empty<object?>();

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

        public IList<IResourceProvider> MergedDictionaries
        {
            get
            {
                if (_mergedDictionaries == null)
                {
                    _mergedDictionaries = new AvaloniaList<IResourceProvider>();
                    _mergedDictionaries.ResetBehavior = ResetBehavior.Remove;
                    _mergedDictionaries.ForEachItem(
                        x =>
                        {
                            if (Owner is object)
                            {
                                x.AddOwner(Owner);
                            }
                        },
                        x =>
                        {
                            if (Owner is object)
                            {
                                x.RemoveOwner(Owner);
                            }
                        }, 
                        () => throw new NotSupportedException("Dictionary reset not supported"));
                }

                return _mergedDictionaries;
            }
        }

        bool IResourceNode.HasResources
        {
            get
            {
                if (_inner?.Count > 0)
                {
                    return true;
                }

                if (_mergedDictionaries?.Count > 0)
                {
                    foreach (var i in _mergedDictionaries)
                    {
                        if (i.HasResources)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        bool ICollection<KeyValuePair<object, object?>>.IsReadOnly => false;

        private Dictionary<object, object?> Inner => _inner ??= new();

        public event EventHandler? OwnerChanged;

        public void Add(object key, object? value)
        {
            Inner.Add(key, value);
            Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
        }

        public void Clear()
        {
            if (_inner?.Count > 0)
            {
                _inner.Clear();
                Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
            }
        }

        public bool ContainsKey(object key) => _inner?.ContainsKey(key) ?? false;

        public bool Remove(object key)
        {
            if (_inner?.Remove(key) == true)
            {
                Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
                return true;
            }

            return false;
        }

        public bool TryGetResource(object key, out object? value)
        {
            if (_inner is not null && _inner.TryGetValue(key, out value))
            {
                return true;
            }

            if (_mergedDictionaries != null)
            {
                for (var i = _mergedDictionaries.Count - 1; i >= 0; --i)
                {
                    if (_mergedDictionaries[i].TryGetResource(key, out value))
                    {
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        public bool TryGetValue(object key, out object? value)
        {
            if (_inner is not null)
                return _inner.TryGetValue(key, out value);
            value = null;
            return false;
        }


        void ICollection<KeyValuePair<object, object?>>.Add(KeyValuePair<object, object?> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<object, object?>>.Contains(KeyValuePair<object, object?> item)
        {
            return (_inner as ICollection<KeyValuePair<object, object?>>)?.Contains(item) ?? false;
        }

        void ICollection<KeyValuePair<object, object?>>.CopyTo(KeyValuePair<object, object?>[] array, int arrayIndex)
        {
            (_inner as ICollection<KeyValuePair<object, object?>>)?.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<object, object?>>.Remove(KeyValuePair<object, object?> item)
        {
            if ((_inner as ICollection<KeyValuePair<object, object?>>)?.Remove(item) == true)
            {
                Owner?.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
                return true;
            }

            return false;
        }

        public IEnumerator<KeyValuePair<object, object?>> GetEnumerator()
        {
            return _inner?.GetEnumerator() ?? Enumerable.Empty<KeyValuePair<object, object?>>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void IResourceProvider.AddOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner != null)
            {
                throw new InvalidOperationException("The ResourceDictionary already has a parent.");
            }
            
            Owner = owner;

            var hasResources = _inner?.Count > 0;
            
            if (_mergedDictionaries is object)
            {
                foreach (var i in _mergedDictionaries)
                {
                    i.AddOwner(owner);
                    hasResources |= i.HasResources;
                }
            }

            if (hasResources)
            {
                owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
            }
        }

        void IResourceProvider.RemoveOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner == owner)
            {
                Owner = null;

                var hasResources = _inner?.Count > 0;

                if (_mergedDictionaries is object)
                {
                    foreach (var i in _mergedDictionaries)
                    {
                        i.RemoveOwner(owner);
                        hasResources |= i.HasResources;
                    }
                }

                if (hasResources)
                {
                    owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Empty);
                }
            }
        }
    }
}
