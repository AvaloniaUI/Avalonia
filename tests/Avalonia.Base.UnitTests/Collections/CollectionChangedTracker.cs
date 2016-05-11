// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;

namespace Avalonia.Base.UnitTests.Collections
{
    internal class CollectionChangedTracker
    {
        public CollectionChangedTracker(INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += CollectionChanged;
        }

        public NotifyCollectionChangedEventArgs Args { get; private set; }

        public void Reset()
        {
            Args = null;
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Args != null)
            {
                throw new Exception("CollectionChanged called more than once.");
            }

            Args = e;
        }
    }
}
