// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reflection;
using Xunit;

[assembly: AssemblyTitle("Avalonia.Markup.Xaml.UnitTests")]

// Don't run tests in parallel.
[assembly: CollectionBehavior(MaxParallelThreads = 1)]
