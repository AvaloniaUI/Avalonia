using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Xunit;

namespace Avalonia.Base.UnitTests;

public class FallbackStorageProviderTests
{
    private static readonly Uri s_folderPath = new(Path.GetTempPath());

    [Fact]
    public async Task Concurrent_Calls_Succeed_When_First_Provider_Turns_Out_Unavailable()
    {
        // Like the DBus portal, which is only known to be unavailable after an asynchronous call.
        var unavailableProvider = new TaskCompletionSource<IStorageProvider?>();
        var target = new FallbackStorageProvider(new Func<Task<IStorageProvider?>>[]
        {
            () => unavailableProvider.Task,
            () => Task.FromResult<IStorageProvider?>(new TestStorageProvider())
        });

        var first = target.TryGetFolderFromPathAsync(s_folderPath);
        var second = target.TryGetFolderFromPathAsync(s_folderPath);
        unavailableProvider.SetResult(null);

        Assert.NotNull(await first);
        Assert.NotNull(await second);
    }

    [Fact]
    public async Task Concurrent_Calls_Invoke_Each_Factory_Once()
    {
        var pendingProvider = new TaskCompletionSource<IStorageProvider?>();
        var factoryCalls = new int[2];
        var target = new FallbackStorageProvider(new Func<Task<IStorageProvider?>>[]
        {
            () =>
            {
                ++factoryCalls[0];
                return pendingProvider.Task;
            },
            () =>
            {
                ++factoryCalls[1];
                return Task.FromResult<IStorageProvider?>(new TestStorageProvider());
            }
        });

        var first = target.TryGetFolderFromPathAsync(s_folderPath);
        var second = target.TryGetFolderFromPathAsync(s_folderPath);
        pendingProvider.SetResult(null);
        await Task.WhenAll(first, second);
        await target.TryGetFolderFromPathAsync(s_folderPath);

        Assert.Equal(new[] { 1, 1 }, factoryCalls);
    }

    [Fact]
    public async Task Failed_Factory_Is_Retried_On_Next_Call()
    {
        var factoryCalls = 0;
        var target = new FallbackStorageProvider(new Func<Task<IStorageProvider?>>[]
        {
            () => ++factoryCalls == 1 ?
                Task.FromException<IStorageProvider?>(new InvalidOperationException()) :
                Task.FromResult<IStorageProvider?>(new TestStorageProvider())
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => target.TryGetFolderFromPathAsync(s_folderPath));

        Assert.NotNull(await target.TryGetFolderFromPathAsync(s_folderPath));
        Assert.Equal(2, factoryCalls);
    }

    private class TestStorageProvider : BclStorageProvider
    {
        public override bool CanOpen => false;
        public override bool CanSave => false;
        public override bool CanPickFolder => false;

        public override Task<OpenFilePickerResult> OpenFilePickerWithResultAsync(FilePickerOpenOptions options)
            => throw new NotSupportedException();

        public override Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options)
            => throw new NotSupportedException();

        public override Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
            => throw new NotSupportedException();
    }
}
