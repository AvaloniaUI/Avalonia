using System;
using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Diagnostics;

[PrivateApi]
[Unstable("Use StyledElementExtensions.GetValueStoreDiagnostic() instead")]
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

[PrivateApi]
[Unstable("Use StyledElementExtensions.GetValueStoreDiagnostic() instead")]
public sealed class AppliedStyle
{
    private readonly StyleInstance _instance;

    internal AppliedStyle(StyleInstance instance)
    {
        _instance = instance;
    }

    public bool HasActivator => _instance.HasActivator;
    public bool IsActive => _instance.IsActive();
    public StyleBase Style => (StyleBase)_instance.Source;
}
