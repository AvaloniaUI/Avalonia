using System;

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

        internal override bool HasSelector => TargetType is not null;

        internal override SelectorMatch Match(IStyleable control, object? host, bool subscribe)
        {
            if (TargetType is null)
                throw new InvalidOperationException("ControlTheme has no TargetType.");

            return control.StyleKey == TargetType ?
                SelectorMatch.AlwaysThisType :
                SelectorMatch.NeverThisType;
        }

        internal override void SetParent(StyleBase? parent)
        {
            throw new InvalidOperationException("ControlThemes cannot be added as a nested style.");
        }
    }
}
