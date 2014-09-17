// -----------------------------------------------------------------------
// <copyright file="Contract.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;

    public static class Contract
    {
        public static void Requires<TException>(bool condition) where TException : Exception, new()
        {
#if DEBUG
            if (!condition)
            {
                throw new TException();
            }
#endif
        }
    }
}
