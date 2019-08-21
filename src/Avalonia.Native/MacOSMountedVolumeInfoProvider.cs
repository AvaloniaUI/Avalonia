using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls.Platform;

namespace Avalonia.Native
{
    internal class WindowsMountedVolumeInfoListener : IDisposable
    {
        private readonly CompositeDisposable _disposables;
        private readonly ObservableCollection<MountedVolumeInfo> _targetObs;
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
            var mountVolInfos = Directory.GetDirectories("/Volumes")
                                .Select(p => new MountedVolumeInfo()
                                {
                                    VolumeLabel = Path.GetFileName(p),
                                    VolumePath = p,
                                    VolumeSizeBytes = 0
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

    public class MacOSMountedVolumeInfoProvider : IMountedVolumeInfoProvider
    {
        public IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            Contract.Requires<ArgumentNullException>(mountedDrives != null);
            return new WindowsMountedVolumeInfoListener(mountedDrives);
        }
    }
}
