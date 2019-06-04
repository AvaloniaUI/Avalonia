// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Xunit;

// Required to avoid InvalidOperationException sometimes thrown
// from Splat.MemoizingMRUCache.cs which is not thread-safe.
// Thrown when trying to access WhenActivated concurrently.
[assembly: CollectionBehavior(DisableTestParallelization = true)]