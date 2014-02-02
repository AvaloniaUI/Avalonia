// -----------------------------------------------------------------------
// <copyright file="Styles.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using Perspex.Controls;

    public class Styles : PerspexList<Style>
    {
        public void Attach(Control control)
        {
            foreach (Style style in this)
            {
                style.Attach(control);
            }
        }
    }
}
