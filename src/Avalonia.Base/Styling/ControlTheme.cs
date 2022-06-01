using System;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a switchable theme for a control.
    /// </summary>
    public class ControlTheme : StyleBase
    {
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
    }
}
