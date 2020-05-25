using System;
using System.Collections.Generic;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines a switchable theme for a control.
    /// </summary>
    public class ControlTheme : StyleBase
    {
        private Styles? _styles;

        /// <summary>
        /// Gets the child styles of the control theme.
        /// </summary>
        public Styles Styles => _styles ??= new Styles(Owner);

        /// <summary>
        /// Gets or sets the control type that the theme applies to.
        /// </summary>
        public Type? TargetType { get; set; }

        protected override IReadOnlyList<IStyle> GetChildrenCore()
        {
            return (IReadOnlyList<IStyle>?)_styles ?? Array.Empty<IStyle>();
        }

        protected override bool GetHasResourcesCore()
        {
            if (ResourcesCore?.Count > 0)
            {
                return true;
            }

            return ((IResourceNode?)_styles)?.HasResources ?? false;
        }

        public override SelectorMatchResult TryAttach(IStyleable target, object? host)
        {
            if (target == host)
            {
                // If target and host are the same control, then we're applying styles to the 
                // control that the theme is applied to.
                Attach(target);
                _styles?.TryAttach(target, host);
                return SelectorMatchResult.AlwaysThisType;
            }
            else
            {
                // If the target is different to the host then we're applying styles to a templated
                // child of the host. The setters in the control theme itself don't apply here: only
                // the child styles.
                return _styles?.TryAttach(target, host) ?? SelectorMatchResult.NeverThisType;
            }
        }
    }
}
