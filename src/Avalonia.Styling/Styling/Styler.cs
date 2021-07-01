using System;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Avalonia.Styling
{
    public class Styler : IStyler
    {
        public void ApplyStyles(IStyleable target)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));

            if (target is IStyleHost styleHost)
            {
                ApplyStyles(target, styleHost, new IStyleWithCancel[] { });
            }
        }

        private void ApplyStyles(IStyleable target, IStyleHost host, IEnumerable<IStyleWithCancel>? cancelStylesFromBelow)
        {
            var parent = host.StylingParent;

            var currentCancelStyles = (cancelStylesFromBelow.Union(host.Styles.OfType<IStyleWithCancel>().Where(style => style.IsCancel))).ToArray();

            if (parent != null)
            {
                ApplyStyles(target, parent, currentCancelStyles);
            }

            if (host.IsStylesInitialized)
            {
                host.Styles.TryAttach(target, host, currentCancelStyles);
            }
        }
    }
}
