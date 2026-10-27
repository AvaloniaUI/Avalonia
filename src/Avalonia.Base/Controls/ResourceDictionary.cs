using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public class ResourceDictionary : ResourceProvider, IResourceDictionary, IThemeVariantProvider
    {
        private object? _lastDeferredItemKey;
        private Dictionary<object, object?>? _inner;
        private AvaloniaList<IResourceProvider>? _mergedDictionaries;
        private AvaloniaDictionary<ThemeVariant, IThemeVariantProvider>? _themeDictionary;
        private ReadOnlyCollection<IResourceProvider>? _readOnlyMergedDictionaries;
        private ReadOnlyDictionary<ThemeVariant, IThemeVariantProvider>? _readOnlyThemeDictionary;
        private bool _isReadOnly;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDictionary"/> class.
        /// </summary>
        public ResourceDictionary() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDictionary"/> class.
        /// </summary>
        public ResourceDictionary(IResourceHost owner) : base(owner) { }

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
                ThrowIfReadOnly();
                Inner[key] = value;
                RaiseResourcesChanged();
            }
        }

        public ICollection<object> Keys => (ICollection<object>?)_inner?.Keys ?? Array.Empty<object>();
        public ICollection<object?> Values => (ICollection<object?>?)_inner?.Values ?? Array.Empty<object?>();

        public IList<IResourceProvider> MergedDictionaries
        {
            get
            {
                if (_isReadOnly)
                {
                    return _readOnlyMergedDictionaries ??= new((IList<IResourceProvider>?)_mergedDictionaries ?? new List<IResourceProvider>());
                }

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
                if (_isReadOnly)
                {
                    return _readOnlyThemeDictionary ??= new(
                        (IDictionary<ThemeVariant, IThemeVariantProvider>?)_themeDictionary
                        ?? new Dictionary<ThemeVariant, IThemeVariantProvider>());
                }

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

        public sealed override bool HasResources
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

        /// <summary>
        /// Gets a value indicating whether the dictionary is read-only.
        /// </summary>
        /// <remarks>
        /// A read-only dictionary throws <see cref="NotSupportedException"/> on any attempt to change its resources,
        /// <see cref="MergedDictionaries"/> or <see cref="ThemeDictionaries"/>.
        /// </remarks>
        public bool IsReadOnly => _isReadOnly;

        private Dictionary<object, object?> Inner => _inner ??= new();
        
        public void Add(object key, object? value)
        {
            ThrowIfReadOnly();
            Inner.Add(key, value);
            RaiseResourcesChanged();
        }

        public void AddDeferred(object key, Func<IServiceProvider?, object?> factory)
            => Add(key, new DeferredItem(factory));

        public void AddDeferred(object key, IDeferredContent deferredContent)
            => Add(key, deferredContent);

        public void AddNotSharedDeferred(object key, IDeferredContent deferredContent)
            => Add(key, new NotSharedDeferredItem(deferredContent));

        public void SetItems(IEnumerable<KeyValuePair<object, object?>> values)
        {
            ThrowIfReadOnly();

            try
            {
                foreach (var value in values)
                    Inner[value.Key] = value.Value;
            }
            finally
            {
                RaiseResourcesChanged();
            }
        }
        
        public void Clear()
        {
            ThrowIfReadOnly();

            if (_inner?.Count > 0)
            {
                _inner.Clear();
                RaiseResourcesChanged();
            }
        }

        public bool ContainsKey(object key) => _inner?.ContainsKey(key) ?? false;

        public bool Remove(object key)
        {
            ThrowIfReadOnly();

            if (_inner?.Remove(key) == true)
            {
                RaiseResourcesChanged();
                return true;
            }

            return false;
        }

        public sealed override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
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
                if (value is IDeferredContent deferred)
                {
                    // Avoid simple reentrancy, which could commonly occur on redefining the resource.
                    if (_lastDeferredItemKey == key)
                    {
                        value = null;
                        return false;
                    }

                    try
                    {
                        _lastDeferredItemKey = key;
                        value = deferred.Build(null) switch
                        {
                            ITemplateResult t => t.Result,
                            { } v => v,
                            _ => null,
                        };
                        if (deferred is not NotSharedDeferredItem)
                        {
                            _inner[key] = value;
                        }
                    }
                    finally
                    {
                        _lastDeferredItemKey = null;
                    }
                }
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Ensures that the resource dictionary can hold up to <paramref name="capacity"/> entries without
        /// any further expansion of its backing storage.
        /// </summary>
        /// <remarks>This method may have no effect when targeting .NET Standard 2.0.</remarks>
        public void EnsureCapacity(int capacity)
        {
            if (_inner is null)
            {
                _inner = new(capacity);
                return;
            }

#if !NETSTANDARD2_0
            Inner.EnsureCapacity(capacity);
#endif
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
            ThrowIfReadOnly();

            if ((_inner as ICollection<KeyValuePair<object, object?>>)?.Remove(item) == true)
            {
                RaiseResourcesChanged();
                return true;
            }

            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Makes the dictionary read-only. This can't be undone.
        /// </summary>
        /// <remarks>
        /// Merged and theme dictionaries are made read-only as well, including dictionaries loaded by ResourceInclude.
        /// Other resource providers in <see cref="MergedDictionaries"/> and <see cref="ThemeDictionaries"/> are not affected.
        /// Deferred resources are still resolved and cached on first access.
        /// </remarks>
        public void MakeReadOnly()
        {
            if (_isReadOnly)
            {
                return;
            }

            _isReadOnly = true;

            if (_mergedDictionaries is not null)
            {
                foreach (var mergedDictionary in _mergedDictionaries)
                {
                    MakeReadOnly(mergedDictionary);
                }
            }

            if (_themeDictionary is not null)
            {
                foreach (var themeDictionary in _themeDictionary.Values)
                {
                    MakeReadOnly(themeDictionary);
                }
            }

            static void MakeReadOnly(IResourceProvider provider)
            {
                if (provider is IResourceInclude include)
                {
                    provider = include.Loaded;
                }

                (provider as ResourceDictionary)?.MakeReadOnly();
            }
        }

        private void ThrowIfReadOnly()
        {
            if (_isReadOnly)
            {
                throw new NotSupportedException("ResourceDictionary is read-only.");
            }
        }

        internal bool ContainsDeferredKey(object key)
        {
            if (_inner is not null && _inner.TryGetValue(key, out var result))
            {
                return result is IDeferredContent;
            }

            return false;
        }

        protected sealed override void OnAddOwner(IResourceHost owner)
        {
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
                owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());
            }
        }

        protected sealed override void OnRemoveOwner(IResourceHost owner)
        {
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
                owner.NotifyHostedResourcesChanged(ResourcesChangedEventArgs.Create());
            }
        }

        private sealed class DeferredItem : IDeferredContent
        {
            private readonly Func<IServiceProvider?,object?> _factory;
            public DeferredItem(Func<IServiceProvider?, object?> factory) => _factory = factory;
            public object? Build(IServiceProvider? serviceProvider) => _factory(serviceProvider);
        }

        private sealed class NotSharedDeferredItem(IDeferredContent deferredContent) : IDeferredContent
        {
            private readonly IDeferredContent _deferredContent = deferredContent ;
            public object? Build(IServiceProvider? serviceProvider) => _deferredContent.Build(serviceProvider);
        }
    }
}
