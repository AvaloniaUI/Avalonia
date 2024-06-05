using System.Collections.Generic;
using Avalonia.Platform.Storage;

namespace Avalonia.Controls.ApplicationLifetimes;

public sealed class FileActivatedEventArgs : ActivatedEventArgs
{
    public FileActivatedEventArgs(IReadOnlyList<IStorageItem> files) : base(ActivationKind.File)
    {
        Files = files;
    }

    public IReadOnlyList<IStorageItem> Files { get; }
}
