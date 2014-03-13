// -----------------------------------------------------------------------
// <copyright file="Styler.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Styler : IStyler
    {
        public void ApplyStyles(IStyleable control)
        {
            IVisual visual = control as IVisual;
            IStyled styleContainer = visual.GetVisualAncestorOrSelf<IStyled>();
            styleContainer.Styles.Attach(control);
        }
    }
}
