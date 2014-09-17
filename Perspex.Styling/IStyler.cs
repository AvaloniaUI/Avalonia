// -----------------------------------------------------------------------
// <copyright file="IStyler.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;

    public interface IStyler
    {
        void ApplyStyles(IStyleable control);
    }
}
