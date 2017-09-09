// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public interface IResourceDictionary : IResourceProvider, IDictionary<object, object>
    {
        /// <summary>
        /// Gets a collection of child resource dictionaries.
        /// </summary>
        IList<IResourceProvider> MergedDictionaries { get; }
    }
}
