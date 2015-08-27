// -----------------------------------------------------------------------
// <copyright file="UserControl.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Styling;

    public class UserControl : ContentControl, IStyleable
    {
        Type IStyleable.StyleKey
        {
            get { return typeof(ContentControl); }
        }
    }
}
