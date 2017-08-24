// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public class ResourceDictionary : AvaloniaDictionary<string, object>, IResourceDictionary
    {
        private AvaloniaList<IResourceDictionary> _mergedDictionaries;

        public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        public ResourceDictionary()
        {
            CollectionChanged += OnCollectionChanged;
        }

        public IList<IResourceDictionary> MergedDictionaries
        {
            get
            {
                if (_mergedDictionaries == null)
                {
                    _mergedDictionaries = new AvaloniaList<IResourceDictionary>();
                    _mergedDictionaries.ResetBehavior = ResetBehavior.Remove;
                    _mergedDictionaries.ForEachItem(
                        x =>
                        {
                            if (x.Count > 0)
                            {
                                OnResourcesChanged();
                            }

                            x.ResourcesChanged += MergedDictionaryResourcesChanged;
                        },
                        x =>
                        {
                            if (x.Count > 0)
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
        public bool TryGetResource(string key, out object value)
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
