using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Logging;
using Avalonia.Threading;

namespace Avalonia.Win32
{
    internal class WindowsMountedVolumeInfoListener : IDisposable
    {
        private readonly CancellationTokenSource _pollCTS;
        private bool _beenDisposed;
        private readonly ObservableCollection<MountedVolumeInfo> _mountedDrives;

        public WindowsMountedVolumeInfoListener(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            _mountedDrives = mountedDrives;

            _pollCTS = new();

            Task.Factory.StartNew(() => Poll(_pollCTS.Token), _pollCTS.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        private async Task Poll(CancellationToken cancellationToken)
        {
            while(true)
            {
                if(cancellationToken.IsCancellationRequested)
                    return;

                var allDrives = DriveInfo.GetDrives();

                var mountVolInfos = allDrives
                                    .Where(p =>
                                    {
                                        try
                                        {
                                            var ret = p.IsReady;
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

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_mountedDrives.SequenceEqual(mountVolInfos))
                        return;
                    else
                    {
                        _mountedDrives.Clear();

                        foreach (var i in mountVolInfos)
                            _mountedDrives.Add(i);
                    }
                });

                await Task.Delay(1000, cancellationToken);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_beenDisposed)
            {
                if (disposing)
                {
                    _pollCTS.Cancel();
                    _pollCTS.Dispose();
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
