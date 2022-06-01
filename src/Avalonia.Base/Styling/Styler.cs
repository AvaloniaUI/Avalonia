using System;

namespace Avalonia.Styling
{
    public class Styler : IStyler
    {
        public void ApplyStyles(IStyleable target)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            // If the control has a themed templated parent then first apply the styles from
            // the templated parent theme.
            if (target.TemplatedParent is IStyleable styleableParent)
                styleableParent.Theme?.TryAttach(target, styleableParent);

            // Next apply the control theme.
            target.Theme?.TryAttach(target, target);

            // Apply styles from the rest of the tree.
            if (target is IStyleHost styleHost)
                ApplyStyles(target, styleHost);
        }

        private void ApplyStyles(IStyleable target, IStyleHost host)
        {
            var parent = host.StylingParent;

            if (parent != null)
                ApplyStyles(target, parent);

            if (host.IsStylesInitialized)
                host.Styles.TryAttach(target, host);
        }
    }
}
