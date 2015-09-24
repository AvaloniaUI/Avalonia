// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Perspex
{
    using System;

    /// <summary>
    /// A stub of Code Contract's Contract class.
    /// </summary>
    /// <remarks>
    /// It would be nice to use Code Contracts on Perspex but last time I tried it slowed things
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
        public static void Requires<TException>(bool condition) where TException : Exception, new()
        {
            if (!condition)
            {
                throw new TException();
            }
        }
    }
}
