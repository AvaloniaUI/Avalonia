using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace Avalonia.Tizen;

internal class NuiClipboardImpl : IClipboardImpl, IDataTransfer, IDataTransferItem
{
    private readonly DataFormat[] _formats;
    private readonly IDataTransferItem[] _items;
    private readonly TextEditor _textEditor;

    public NuiClipboardImpl()
    {
        _formats = [DataFormat.Text];
        _items = [this];

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

    IReadOnlyList<DataFormat> IDataTransfer.Formats
        => _formats;

    IReadOnlyList<DataFormat> IDataTransferItem.Formats
        => _formats;

    IReadOnlyList<IDataTransferItem> IDataTransfer.Items
        => _items;

    public Task ClearAsync()
        => SetTextAsync(string.Empty);

    public Task<IDataTransfer?> TryGetDataAsync()
        => Task.FromResult<IDataTransfer?>(this);

    public async Task SetDataAsync(IDataTransfer dataTransfer)
    {
        var text = await dataTransfer.TryGetTextAsync();
        await SetTextAsync(text ?? string.Empty);
    }

    public Task<object?> TryGetAsync(DataFormat format)
        => DataFormat.Text.Equals(format) ? GetTextAsync() : Task.FromResult<object?>(null);

    private Task<object?> GetTextAsync()
    {
        _textEditor.Show();
        _textEditor.Text = "";

        //The solution suggested by Samsung, The method PasteTo will execute async and need delay
        TextUtils.PasteTo(_textEditor);

        return Task.Run<object?>(async () =>
        {
            await Task.Delay(10);

            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _textEditor.Hide();
                return _textEditor.Text;
            });
        });
    }

    private Task SetTextAsync(string text)
    {
        _textEditor.Show();
        _textEditor.Text = text;

        //The solution suggested by Samsung, The method SelectWholeText will execute async and need delay
        _textEditor.SelectWholeText();

        return Task.Run(async () =>
        {
            await Task.Delay(10);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TextUtils.CopyToClipboard(_textEditor);
                _textEditor.Hide();
            });
        });
    }

    void IDisposable.Dispose()
    {
    }
}
