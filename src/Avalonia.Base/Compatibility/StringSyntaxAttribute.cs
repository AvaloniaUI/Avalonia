#pragma warning disable MA0048 // File name must match type name
// https://github.com/dotnet/runtime/blob/v8.0.4/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/StringSyntaxAttribute.cs

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis
{
#if !NET7_0_OR_GREATER
    /// <summary>Specifies the syntax used in a string.</summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class StringSyntaxAttribute : Attribute
    {
        /// <summary>Initializes the <see cref="StringSyntaxAttribute"/> with the identifier of the syntax used.</summary>
        /// <param name="syntax">The syntax identifier.</param>
        public StringSyntaxAttribute(string syntax)
        {
            Syntax = syntax;
            Arguments = Array.Empty<object?>();
        }

        /// <summary>Initializes the <see cref="StringSyntaxAttribute"/> with the identifier of the syntax used.</summary>
        /// <param name="syntax">The syntax identifier.</param>
        /// <param name="arguments">Optional arguments associated with the specific syntax employed.</param>
        public StringSyntaxAttribute(string syntax, params object?[] arguments)
        {
            Syntax = syntax;
            Arguments = arguments;
        }

        /// <summary>Gets the identifier of the syntax used.</summary>
        public string Syntax { get; }

        /// <summary>Optional arguments associated with the specific syntax employed.</summary>
        public object?[] Arguments { get; }

        /// <summary>The syntax identifier for strings containing XML.</summary>
        public const string Xml = nameof(Xml);
    }
#endif
}
