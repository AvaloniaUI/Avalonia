using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Metadata;
using Avalonia.PropertyStore;
using Avalonia.Styling;

namespace Avalonia.Diagnostics;

internal class StyleValueFrameDiagnostic : IValueFrameDiagnostic
{
    private readonly StyleInstance _styleInstance;

    internal StyleValueFrameDiagnostic(StyleInstance styleInstance)
    {
        _styleInstance = styleInstance;
    }

    public object? Source => _styleInstance.Source;

    public IValueFrameDiagnostic.FrameType Type => _styleInstance.Source switch
    {
        Style => IValueFrameDiagnostic.FrameType.Style,
        ControlTheme => IValueFrameDiagnostic.FrameType.Theme,
        _ => IValueFrameDiagnostic.FrameType.Unknown
    };

    public bool IsActive => _styleInstance.IsActive();
    public BindingPriority Priority => _styleInstance.FramePriority.ToBindingPriority();
    public IEnumerable<ValueEntryDiagnostic> Values
    {
        get
        {
            foreach (var setter in ((StyleBase)_styleInstance.Source!).Setters)
            {
                if (setter is Setter { Property: not null } regularSetter)
                {
                    yield return new ValueEntryDiagnostic(regularSetter.Property, regularSetter.Value);
                }
            }
        }
    }

    [Unstable("Compatibility with 11.x")]
    public AppliedStyle AsAppliedStyle() => new AppliedStyle(_styleInstance);
}
