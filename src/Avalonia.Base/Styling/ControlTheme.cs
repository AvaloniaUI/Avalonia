using System;
using Avalonia.PropertyStore;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a switchable theme for a control.
    /// </summary>
    public class ControlTheme : StyleBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlTheme"/> class.
        /// </summary>
        public ControlTheme() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlTheme"/> class.
        /// </summary>
        /// <param name="targetType">The value for <see cref="TargetType"/>.</param>
        public ControlTheme(Type targetType) => TargetType = targetType;

        /// <summary>
        /// Gets or sets the type for which this control theme is intended.
        /// </summary>
        public Type? TargetType { get; set; }

        /// <summary>
        /// Gets or sets a control theme that is the basis of the current theme.
        /// </summary>
        public ControlTheme? BasedOn { get; set; }

        public override SelectorMatchResult TryAttach(IStyleable target, object? host)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            if (TargetType is null)
                throw new InvalidOperationException("ControlTheme has no TargetType.");

            var result = BasedOn?.TryAttach(target, host) ?? SelectorMatchResult.NeverThisType;

            if (HasSettersOrAnimations && TargetType.IsAssignableFrom(target.StyleKey))
            {
                Attach(target, null);
                result = SelectorMatchResult.AlwaysThisType;
            }

            var childResult = TryAttachChildren(target, host);

            if (childResult > result)
                result = childResult;

            return result;
        }

        public override string ToString()
        {
            if (TargetType is not null)
                return "ControlTheme: " + TargetType.Name;
            else
                return "ControlTheme";
        }

        internal override void SetParent(StyleBase? parent)
        {
            throw new InvalidOperationException("ControlThemes cannot be added as a nested style.");
        }
    }
}
