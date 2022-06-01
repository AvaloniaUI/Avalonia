using System;

#nullable enable

namespace Avalonia.Styling
{
    public class Styler : IStyler
    {
        public void ApplyStyles(IStyleable target)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));

            // If the control has a themed templated parent then first apply the styles from
            // the templated parent theme.
            if (target.TemplatedParent is IThemed themedTemplatedParent)
            {
                themedTemplatedParent.Theme?.TryAttach(target, themedTemplatedParent);
            }

            // If the control itself is themed, then next apply the control theme.
            if (target is IThemed themed)
            {
                themed.Theme?.TryAttach(target, target);
            }

            // Apply styles from the rest of the tree.
            if (target is IStyleHost styleHost)
            {
                ApplyStyles(target, styleHost);
            }
        }

        private void ApplyStyles(IStyleable target, IStyleHost host)
        {
            var parent = host.StylingParent;

            if (parent != null)
            {
                ApplyStyles(target, parent);
            }

            if (host.IsStylesInitialized)
            {
                host.Styles.TryAttach(target, host);
            }
        }
    }
}
