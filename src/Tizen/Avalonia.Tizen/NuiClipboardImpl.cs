using Avalonia.Input;
using Avalonia.Input.Platform;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using FocusManager = Tizen.NUI.FocusManager;

namespace Avalonia.Tizen;

internal class NuiClipboardImpl : IClipboard
{
    private TextEditor _textEditor;
    public NuiClipboardImpl()
    {
        _textEditor = new TextEditor()
        {
            HeightResizePolicy = ResizePolicyType.Fixed,
            WidthResizePolicy = ResizePolicyType.Fixed,
            Position = new Position(-1000, -1000),
            Size = new(1, 1)
        };
        
        Window.Instance.GetDefaultLayer().Add(_textEditor);
        _textEditor.LowerToBottom();
    }

    public Task ClearAsync() =>
        SetTextAsync("");

    public Task<string?> GetTextAsync()
    {
        _textEditor.Show();
        _textEditor.Text = "";
        TextUtils.PasteTo(_textEditor);
        _textEditor.Hide();
        return Task.FromResult<string?>(_textEditor.Text);
    }

    public async Task SetTextAsync(string? text)
    {
        _textEditor.Show();
        FocusManager.Instance.SetCurrentFocusView(_textEditor);
        _textEditor.Text = text;
        _textEditor.SelectWholeText();
        await Task.Delay(1);
        TextUtils.CopyToClipboard(_textEditor);
        _textEditor.Hide();
    }

    public Task<object?> GetDataAsync(string format) =>
        throw new PlatformNotSupportedException();

    public Task SetDataObjectAsync(IDataObject data) =>
        throw new PlatformNotSupportedException();

    public Task<string[]> GetFormatsAsync() =>
        throw new PlatformNotSupportedException();
}
