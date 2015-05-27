// -----------------------------------------------------------------------
// <copyright file="Styles.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using Perspex.Collections;

    public class Styles : PerspexList<IStyle>, IStyle
    {
        public void Attach(IStyleable control)
        {
            foreach (IStyle style in this)
            {
                style.Attach(control);
            }
        }
    }
}
