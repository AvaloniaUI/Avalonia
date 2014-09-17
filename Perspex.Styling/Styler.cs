// -----------------------------------------------------------------------
// <copyright file="Styler.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using System.Linq;
    using Splat;

    public class Styler : IStyler
    {
        public void ApplyStyles(IStyleable control)
        {
            IVisual visual = control as IVisual;
            IStyleHost styleContainer = visual
                .GetSelfAndVisualAncestors()
                .OfType<IStyleHost>()
                .FirstOrDefault();
            IGlobalStyle global = Locator.Current.GetService<IGlobalStyle>();

            if (global != null)
            {
                global.Attach(control);
            }

            if (styleContainer != null)
            {
                this.ApplyStyles(control, styleContainer);
            }
        }

        private void ApplyStyles(IStyleable control, IStyleHost container)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Contract.Requires<ArgumentNullException>(container != null);

            IVisual visual = container as IVisual;

            if (visual != null)
            {
                IStyleHost parentContainer = visual
                    .GetVisualAncestors()
                    .OfType<IStyleHost>()
                    .FirstOrDefault();

                if (parentContainer != null)
                {
                    this.ApplyStyles(control, parentContainer);
                }
            }

            container.Styles.Attach(control);
        }

        private IStyleHost GetParentContainer(IStyleHost container)
        {
            return container.GetVisualAncestors().OfType<IStyleHost>().FirstOrDefault();
        }
    }
}
