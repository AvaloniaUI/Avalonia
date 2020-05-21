using System;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Applies styles to controls based on styles found in themes and styles in the logical tree.
    /// </summary>
    public class Styler : IStyler
    {
        /// <summary>
        /// Applies all relevant styles to a control.
        /// </summary>
        /// <param name="target">The control to be styled.</param>
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
            if (target is IStyleHost host)
            {
                ApplyStyles(target, host);
            }
        }

        private void ApplyStyles(IStyleable target, IStyleHost host)
        {
            var parent = host.StylingParent;

            // Later styles have precedence so styles are applied from the root of the tree up
            // towards the control being styled.
            if (parent != null)
            {
                ApplyStyles(target, parent);
            }

            if (host.HasStyles)
            {
                host.Styles.TryAttach(target, host);
            }
        }
    }
}
