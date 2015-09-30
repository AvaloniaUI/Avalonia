// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Perspex.VisualTree;

namespace Perspex.Styling
{
    public class Styler : IStyler
    {
        public void ApplyStyles(IStyleable control)
        {
            IVisual visual = control as IVisual;
            IStyleHost styleContainer = visual
                .GetSelfAndVisualAncestors()
                .OfType<IStyleHost>()
                .FirstOrDefault();
            IGlobalStyles global = PerspexLocator.Current.GetService<IGlobalStyles>();

            if (global != null)
            {
                global.Styles.Attach(control, null);
            }

            if (styleContainer != null)
            {
                ApplyStyles(control, styleContainer);
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
                    ApplyStyles(control, parentContainer);
                }
            }

            container.Styles.Attach(control, container);
        }

        private IStyleHost GetParentContainer(IStyleHost container)
        {
            return container.GetVisualAncestors().OfType<IStyleHost>().FirstOrDefault();
        }
    }
}
