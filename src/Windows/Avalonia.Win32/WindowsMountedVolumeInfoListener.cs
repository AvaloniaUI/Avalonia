using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls.Platform;

namespace Avalonia.Win32
{
    internal class WindowsMountedVolumeInfoListener : IDisposable
    {
        private readonly CompositeDisposable _disposables;        
        private bool _beenDisposed = false;
        private ObservableCollection<MountedVolumeInfo> mountedDrives;

        public WindowsMountedVolumeInfoListener(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            this.mountedDrives = mountedDrives;
            _disposables = new CompositeDisposable();

            var pollTimer = Observable.Interval(TimeSpan.FromSeconds(1))
                                      .Subscribe(Poll);

            _disposables.Add(pollTimer);

            Poll(0);
        }

        private void Poll(long _)
        {
            var allDrives = DriveInfo.GetDrives();

            var mountVolInfos = allDrives
                                .Where(p => p.IsReady)
                                .Select(p => new MountedVolumeInfo()
                                {
                                    VolumeLabel = string.IsNullOrEmpty(p.VolumeLabel.Trim()) ? p.RootDirectory.FullName 
                                                                                             : $"{p.VolumeLabel} ({p.Name})",
                                    VolumePath = p.RootDirectory.FullName,
                                    VolumeSizeBytes = (ulong)p.TotalSize
                                })
                                .ToArray();

            if (mountedDrives.SequenceEqual(mountVolInfos))
                return;
            else
            {
                mountedDrives.Clear();

                foreach (var i in mountVolInfos)
                    mountedDrives.Add(i);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_beenDisposed)
            {
                if (disposing)
                {

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
