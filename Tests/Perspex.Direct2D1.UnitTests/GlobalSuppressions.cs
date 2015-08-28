// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    "StyleCop.CSharp.MaintainabilityRules",
    "SA1401:Fields must be private",
    Justification = "PerspexProperty fields should not be private.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    "StyleCop.CSharp.DocumentationRules",
    "SA1600:Elements must be documented",
    Justification = "Tests should be self-documenting")]