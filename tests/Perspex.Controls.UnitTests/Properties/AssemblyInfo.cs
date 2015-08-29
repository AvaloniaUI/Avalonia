// -----------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using Xunit;

[assembly: AssemblyTitle("Perspex.Controls.UnitTests")]

// Don't run tests in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]