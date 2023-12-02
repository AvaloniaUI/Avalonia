using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Styling;

namespace Avalonia.Diagnostics;

internal sealed class ValueFrameDiagnostic : IValueFrameDiagnostic
{
    private readonly ValueFrame _valueFrame;

    internal ValueFrameDiagnostic(ValueFrame valueFrame)
    {
        _valueFrame = valueFrame;
    }

    public string? Description => (_valueFrame.Owner?.Owner as StyledElement)?.StyleKey.Name;

    public IValueFrameDiagnostic.FrameType Type => IValueFrameDiagnostic.FrameType.Template;

    public bool IsActive => _valueFrame.IsActive();
    public BindingPriority Priority => _valueFrame.FramePriority.ToBindingPriority();
    public IEnumerable<ValueEntryDiagnostic> Values
    {
        get
        {
            for (var i = 0; i < _valueFrame.EntryCount; i++)
            {
                var entry = _valueFrame.GetEntry(i);
                if (entry.HasValue())
                {
                    yield return new ValueEntryDiagnostic(entry.Property, entry.GetValue());
                }
            }
        }
    }
}
