using Avalonia.Input;
using Avalonia.Input.Platform;
using Tizen.NUI.BaseComponents;

namespace Avalonia.Tizen;

internal class NuiClipboardImpl : IClipboard
{
    private TextEditor _textEditor;
    public NuiClipboardImpl()
    {
        _textEditor = new TextEditor();
    }

    public Task ClearAsync() => 
        SetTextAsync("");

    public Task<string?> GetTextAsync()
    {
        _textEditor.Text = "";
        TextUtils.PasteTo(_textEditor);
        return Task.FromResult<string?>(_textEditor.Text);
    }
    
    public Task SetTextAsync(string? text)
    {
        _textEditor.Text = text;
        _textEditor.SelectWholeText();
        TextUtils.CopyToClipboard(_textEditor);

        return Task.CompletedTask;
    }

    public Task<object?> GetDataAsync(string format) =>
        throw new PlatformNotSupportedException();

    public Task SetDataObjectAsync(IDataObject data) =>
        throw new PlatformNotSupportedException();

    public Task<string[]> GetFormatsAsync() =>
        throw new PlatformNotSupportedException();
}
