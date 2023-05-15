using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public class ResourceDictionary : IResourceDictionary, IThemeVariantProvider
    {
        private object? lastDeferredItemKey;
        private Dictionary<object, object?>? _inner;
        private IResourceHost? _owner;
        private AvaloniaList<IResourceProvider>? _mergedDictionaries;
        private AvaloniaDictionary<ThemeVariant, IThemeVariantProvider>? _themeDictionary;

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
            get
            {
                TryGetValue(key, out var value);
                return value;
            }
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
                            if (Owner is not null)
                            {
                                x.AddOwner(Owner);
                            }
                        },
                        x =>
                        {
                            if (Owner is not null)
                            {
                                x.RemoveOwner(Owner);
                            }
                        }, 
                        () => throw new NotSupportedException("Dictionary reset not supported"));
                }

                return _mergedDictionaries;
            }
        }

        public IDictionary<ThemeVariant, IThemeVariantProvider> ThemeDictionaries
        {
            get
            {
                if (_themeDictionary == null)
                {
                    _themeDictionary = new AvaloniaDictionary<ThemeVariant, IThemeVariantProvider>(2);
                    _themeDictionary.ForEachItem(
                        (_, x) =>
                        {
                            if (Owner is not null)
                            {
                                x.AddOwner(Owner);
                            }
                        },
                        (_, x) =>
                        {
                            if (Owner is not null)
                            {
                                x.RemoveOwner(Owner);
                            }
                        },
                        () => throw new NotSupportedException("Dictionary reset not supported"));
                }
                return _themeDictionary;
            }
        }
        
        ThemeVariant? IThemeVariantProvider.Key { get; set; }

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

        public void AddDeferred(object key, Func<IServiceProvider?, object?> factory)
        {
            Inner.Add(key, new DeferredItem(factory));
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

        public bool TryGetResource(object key, ThemeVariant? theme, out object? value)
        {
            if (TryGetValue(key, out value))
                return true;

            if (_themeDictionary is not null)
            {
                IThemeVariantProvider? themeResourceProvider;
                if (theme is not null && theme != ThemeVariant.Default)
                {
                    if (_themeDictionary.TryGetValue(theme, out themeResourceProvider)
                        && themeResourceProvider.TryGetResource(key, theme, out value))
                    {
                        return true;
                    }

                    var themeInherit = theme.InheritVariant;
                    while (themeInherit is not null)
                    {
                        if (_themeDictionary.TryGetValue(themeInherit, out themeResourceProvider)
                            && themeResourceProvider.TryGetResource(key, theme, out value))
                        {
                            return true;
                        }
        
                        themeInherit = themeInherit.InheritVariant;
                    }
                }

                if (_themeDictionary.TryGetValue(ThemeVariant.Default, out themeResourceProvider)
                    && themeResourceProvider.TryGetResource(key, theme, out value))
                {
                    return true;
                }
            }

            if (_mergedDictionaries != null)
            {
                for (var i = _mergedDictionaries.Count - 1; i >= 0; --i)
                {
                    if (_mergedDictionaries[i].TryGetResource(key, theme, out value))
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
            if (_inner is not null && _inner.TryGetValue(key, out value))
            {
                if (value is DeferredItem deffered)
                {
                    // Avoid simple reentrancy, which could commonly occur on redefining the resource.
                    if (lastDeferredItemKey == key)
                    {
                        value = null;
                        return false;
                    }

                    try
                    {
                        lastDeferredItemKey = key;
                        _inner[key] = value = deffered.Factory(null) switch
                        {
                            ITemplateResult t => t.Result,
                            { } v => v,
                            _ => null,
                        };
                    }
                    finally
                    {
                        lastDeferredItemKey = null;
                    }
                }
                return true;
            }

            value = null;
            return false;
        }

        public IEnumerator<KeyValuePair<object, object?>> GetEnumerator()
        {
            return _inner?.GetEnumerator() ?? Enumerable.Empty<KeyValuePair<object, object?>>().GetEnumerator();
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

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal bool ContainsDeferredKey(object key)
        {
            if (_inner is not null && _inner.TryGetValue(key, out var result))
            {
                return result is DeferredItem;
            }

            return false;
        }

        void IResourceProvider.AddOwner(IResourceHost owner)
        {
            owner = owner ?? throw new ArgumentNullException(nameof(owner));

            if (Owner != null)
            {
                throw new InvalidOperationException("The ResourceDictionary already has a parent.");
            }
            
            Owner = owner;

            var hasResources = _inner?.Count > 0;
            
            if (_mergedDictionaries is not null)
            {
                foreach (var i in _mergedDictionaries)
                {
                    i.AddOwner(owner);
                    hasResources |= i.HasResources;
                }
            }
            if (_themeDictionary is not null)
            {
                foreach (var i in _themeDictionary.Values)
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

                if (_mergedDictionaries is not null)
                {
                    foreach (var i in _mergedDictionaries)
                    {
                        i.RemoveOwner(owner);
                        hasResources |= i.HasResources;
                    }
                }
                if (_themeDictionary is not null)
                {
                    foreach (var i in _themeDictionary.Values)
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

        private class DeferredItem
        {
            public DeferredItem(Func<IServiceProvider?, object?> factory) => Factory = factory;
            public Func<IServiceProvider?, object?> Factory { get; }
        }
    }
}
