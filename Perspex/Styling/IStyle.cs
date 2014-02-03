// -----------------------------------------------------------------------
// <copyright file="IStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using Perspex.Controls;

    public interface IStyle
    {
        void Attach(IStyleable control);
    }
}
