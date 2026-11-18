using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// The semantic role of a transient input decoration. The control's theme maps it to a concrete
    /// visual (brush and underline/background style), so decorations respect the control palette.
    /// </summary>
    [Unstable]
    public enum TextInputDecorationKind
    {
        /// <summary>Raw, unconverted composition input.</summary>
        Input,

        /// <summary>A converted composition clause that is not the active target.</summary>
        Converted,

        /// <summary>The active conversion target - the clause the next candidate replaces.</summary>
        ConvertedTarget,

        /// <summary>A target clause that has not yet been converted.</summary>
        TargetNotConverted,

        /// <summary>An input error clause.</summary>
        InputError,

        /// <summary>A range of committed content the input method is reconverting.</summary>
        ReconversionTarget
    }
}
