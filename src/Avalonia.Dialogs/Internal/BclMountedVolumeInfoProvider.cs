using System;
using System.Collections.ObjectModel;
using System.IO;
using Avalonia.Controls.Platform;
using Avalonia.Reactive;

namespace Avalonia.Dialogs.Internal;

internal class BclMountedVolumeInfoProvider : IMountedVolumeInfoProvider
{
    public IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives)
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            string directory;
            ulong totalSize;
            try
            {
                if (!drive.IsReady)
                    continue;
                totalSize = (ulong)drive.TotalSize;
                directory = drive.RootDirectory.FullName;

                _ = new DirectoryInfo(directory).EnumerateFileSystemInfos();
            }
            catch
            {
                continue;
            }

            mountedDrives.Add(new MountedVolumeInfo
            {
                VolumeLabel = string.IsNullOrEmpty(drive.VolumeLabel.Trim()) ? directory : drive.VolumeLabel,
                VolumePath = directory,
                VolumeSizeBytes = totalSize
            });
        }
        return Disposable.Empty;
    }
}
