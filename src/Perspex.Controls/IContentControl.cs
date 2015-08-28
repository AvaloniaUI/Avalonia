// -----------------------------------------------------------------------
// <copyright file="IContentControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------
namespace Perspex.Controls
{
    using Perspex.Layout;

    public interface IContentControl : IControl
    {
        object Content { get; set; }

        HorizontalAlignment HorizontalContentAlignment { get; set; }

        VerticalAlignment VerticalContentAlignment { get; set; }
    }
}