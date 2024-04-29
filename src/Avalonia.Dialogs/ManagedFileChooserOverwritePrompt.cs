using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.Dialogs;

public class ManagedFileChooserOverwritePrompt : TemplatedControl
{
    internal event Action<bool>? Result;

    private string _fileName = "";

    public static readonly DirectProperty<ManagedFileChooserOverwritePrompt, string> FileNameProperty = AvaloniaProperty.RegisterDirect<ManagedFileChooserOverwritePrompt, string>(
        "FileName", o => o.FileName, (o, v) => o.FileName = v);

    public string FileName
    {
        get => _fileName;
        set => SetAndRaise(FileNameProperty, ref _fileName, value);
    }

    public void Confirm()
    {
        Result?.Invoke(true);
    }

    public void Cancel()
    {
        Result?.Invoke(false);
    }
}
