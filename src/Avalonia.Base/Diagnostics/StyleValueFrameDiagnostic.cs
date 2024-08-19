using System.Collections.Generic;
using Avalonia.Data;
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

    public string? Description => _styleInstance.Source switch
    {
        Style s => GetFullSelector(s),
        ControlTheme t => t.TargetType?.Name,
        _ => null
    };

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

    private string GetFullSelector(Style? style)
    {
        var selectors = new Stack<string>();

        while (style is not null)
        {
            if (style.Selector is not null)
            {
                selectors.Push(style.Selector.ToString());
            }
            
            style = style.Parent as Style;
        }

        return string.Concat(selectors);
    }
}
