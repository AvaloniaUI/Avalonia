using System.Collections.Generic;
using Avalonia.Styling;

namespace Avalonia.Diagnostics
{
    /// <summary>
    /// Contains information about style related diagnostics of a control.
    /// </summary>
    public class StyleDiagnostics
    {
        /// <summary>
        /// Currently applied styles.
        /// </summary>
        public IReadOnlyList<AppliedStyle> AppliedStyles { get; }

        public StyleDiagnostics(IReadOnlyList<AppliedStyle> appliedStyles)
        {
            AppliedStyles = appliedStyles;
        }
    }
}
