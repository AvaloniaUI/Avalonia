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
        private readonly ObservableCollection<MountedVolumeInfo> _targetObs = new ObservableCollection<MountedVolumeInfo>();
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
                                .Select(p => new MountedVolumeInfo()
                                {
                                    VolumeLabel = p.VolumeLabel,
                                    VolumePath = p.RootDirectory.FullName,
                                    VolumeSizeBytes = (ulong)p.TotalSize
                                })
                                .ToArray();

            if (_targetObs.SequenceEqual(mountVolInfos))
                return;
            else
            {
                _targetObs.Clear();

                foreach (var i in mountVolInfos)
                    _targetObs.Add(i);
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
