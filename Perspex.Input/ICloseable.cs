// -----------------------------------------------------------------------
// <copyright file="ICloseable.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;

    public interface ICloseable
    {
        event EventHandler Closed;
    }
}
