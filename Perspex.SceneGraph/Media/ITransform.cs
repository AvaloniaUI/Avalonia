// -----------------------------------------------------------------------
// <copyright file="ITransform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;

    public interface ITransform
    {
        event EventHandler Changed;

        Matrix Value { get; }
    }
}
