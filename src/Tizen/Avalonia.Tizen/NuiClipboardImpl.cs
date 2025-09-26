using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace Avalonia.Tizen;

internal class NuiClipboardImpl : IClipboardImpl, IAsyncDataTransfer, IAsyncDataTransferItem
{
    private readonly DataFormat[] _formats;
    private readonly IAsyncDataTransferItem[] _items;
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

    IReadOnlyList<DataFormat> IAsyncDataTransfer.Formats
        => _formats;

    IReadOnlyList<DataFormat> IAsyncDataTransferItem.Formats
        => _formats;

    IReadOnlyList<IAsyncDataTransferItem> IAsyncDataTransfer.Items
        => _items;

    public Task ClearAsync()
        => SetTextAsync(string.Empty);

    public Task<IAsyncDataTransfer?> TryGetDataAsync()
        => Task.FromResult<IAsyncDataTransfer?>(this);

    public async Task SetDataAsync(IAsyncDataTransfer dataTransfer)
    {
        var text = await dataTransfer.TryGetTextAsync();
        await SetTextAsync(text ?? string.Empty);
    }

    public Task<object?> TryGetRawAsync(DataFormat format)
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
