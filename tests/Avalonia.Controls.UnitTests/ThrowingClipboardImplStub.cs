using System;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Controls.UnitTests;

internal sealed class ThrowingClipboardImplStub(Type exceptionType) : IClipboardImpl
{
    public int SetDataCount { get; private set; }

    public int TryGetDataCount { get; private set; }

    public Task<IAsyncDataTransfer?> TryGetDataAsync()
    {
        ++TryGetDataCount;
        return Task.FromException<IAsyncDataTransfer?>(CreateException());
    }

    public Task SetDataAsync(IAsyncDataTransfer dataTransfer)
    {
        ++SetDataCount;
        return Task.FromException(CreateException());
    }

    public Task ClearAsync()
        => Task.FromException(CreateException());

    private Exception CreateException()
        => (Exception)Activator.CreateInstance(exceptionType)!;
}
