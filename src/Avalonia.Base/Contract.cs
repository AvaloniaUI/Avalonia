// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Avalonia
{
    /// <summary>
    /// A stub of Code Contract's Contract class.
    /// </summary>
    /// <remarks>
    /// It would be nice to use Code Contracts on Avalonia but last time I tried it slowed things
    /// to a crawl and often crashed. Instead use the same signature for checking preconditions
    /// in the hope that it might become usable at some point.
    /// </remarks>
    public static class Contract
    {
        /// <summary>
        /// Specifies a precondition.
        /// </summary>
        /// <typeparam name="TException">
        /// The exception to throw if <paramref name="condition"/> is false.
        /// </typeparam>
        /// <param name="condition">The precondition.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ContractAnnotation("condition:false=>stop")]        
        public static void Requires<TException>([DoesNotReturnIf(false)] bool condition) where TException : Exception, new()
        {
            if (!condition)
            {
                throw new TException();
            }
        }

        /// <summary>
        /// Specifies a precondition.
        /// </summary>
        /// <typeparam name="TException">
        /// The exception to throw if <paramref name="condition"/> is false.
        /// </typeparam>
        /// <param name="obj">The precondition.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [ContractAnnotation("condition:false=>stop")]
        public static void RequireNotNull([EnsuresNotNull] object obj)
        {
            if (obj is null)
            {
                throw new ArgumentNullException();
            }
        }
    }

    internal class EnsuresNotNullAttribute : Attribute
    {
    }

    /// <summary>Specifies that the method will not return if the associated Boolean parameter is passed the specified value.</summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class DoesNotReturnIfAttribute : Attribute
    {
        /// <summary>Initializes the attribute with the specified parameter value.</summary>
        /// <param name="parameterValue">
        /// The condition parameter value. Code after the method will be considered unreachable by diagnostics if the argument to
        /// the associated parameter matches this value.
        /// </param>
        public DoesNotReturnIfAttribute(bool parameterValue) => ParameterValue = parameterValue;

        /// <summary>Gets the condition parameter value.</summary>
        public bool ParameterValue { get; }
    }
}
