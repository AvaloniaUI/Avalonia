// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public class ResourceDictionary : AvaloniaDictionary<object, object>, IResourceDictionary
    {
        private AvaloniaList<IResourceProvider> _mergedDictionaries;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceDictionary"/> class.
        /// </summary>
        public ResourceDictionary()
        {
            CollectionChanged += OnCollectionChanged;
        }

        /// <inheritdoc/>
        public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        /// <inheritdoc/>
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
                            if (x.HasResources)
                            {
                                OnResourcesChanged();
                            }

                            x.ResourcesChanged += MergedDictionaryResourcesChanged;
                        },
                        x =>
                        {
                            if (x.HasResources)
                            {
                                OnResourcesChanged();
                            }

                            x.ResourcesChanged -= MergedDictionaryResourcesChanged;
                        },
                        () => { });
                }

                return _mergedDictionaries;
            }
        }

        /// <inheritdoc/>
        bool IResourceProvider.HasResources
        {
            get => Count > 0 || (_mergedDictionaries?.Any(x => x.HasResources) ?? false);
        }

        /// <inheritdoc/>
        public bool TryGetResource(object key, out object value)
        {
            if (TryGetValue(key, out value))
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

            return false;
        }

        private void OnResourcesChanged()
        {
            ResourcesChanged?.Invoke(this, new ResourcesChangedEventArgs());
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => OnResourcesChanged();
        private void MergedDictionaryResourcesChanged(object sender, ResourcesChangedEventArgs e) => OnResourcesChanged();
    }
}
