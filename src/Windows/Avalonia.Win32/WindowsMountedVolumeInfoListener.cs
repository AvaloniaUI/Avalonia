using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls.Platform;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.Win32
{
    internal class WindowsMountedVolumeInfoListener : IDisposable
    {
        private readonly IDisposable _disposable;
        private bool _beenDisposed;
        private readonly ObservableCollection<MountedVolumeInfo> _mountedDrives;

        public WindowsMountedVolumeInfoListener(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            _mountedDrives = mountedDrives;

            _disposable = DispatcherTimer.Run(Poll, TimeSpan.FromSeconds(1));

            Poll();
        }

        private bool Poll()
        {
            var allDrives = DriveInfo.GetDrives();

            var mountVolInfos = allDrives
                                .Where(p =>
                                {
                                    try
                                    {
                                        var ret = p.IsReady;
                                        _ = p.TotalSize; // try to read size as a proof of read access.
                                        return ret;
                                    }
                                    catch (Exception e)
                                    {
                                        Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(this, $"Error in Windows drive enumeration: {e.Message}");
                                    }
                                    return false;
                                })
                                .Select(p => new MountedVolumeInfo()
                                {
                                    VolumeLabel = string.IsNullOrEmpty(p.VolumeLabel.Trim()) ? p.RootDirectory.FullName
                                                                                             : $"{p.VolumeLabel} ({p.Name})",
                                    VolumePath = p.RootDirectory.FullName,
                                    VolumeSizeBytes = (ulong)p.TotalSize
                                })
                                .ToArray();

            if (_mountedDrives.SequenceEqual(mountVolInfos))
                return true;
            else
            {
                _mountedDrives.Clear();

                foreach (var i in mountVolInfos)
                    _mountedDrives.Add(i);
                return true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_beenDisposed)
            {
                if (disposing)
                {
                    _disposable.Dispose();
                }
                _beenDisposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
