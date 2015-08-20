// -----------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1401:Fields must be private",
    Justification = "Routed event fields should not be private.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1609:Property documentation must have value",
    Justification = "This rule is fscking pointless")]