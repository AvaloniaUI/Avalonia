using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls.Platform;

namespace Avalonia.Native
{
    internal class MacOSMountedVolumeInfoListener : IDisposable
    {
        private readonly CompositeDisposable _disposables;
        private bool _beenDisposed = false;
        private ObservableCollection<MountedVolumeInfo> mountedDrives;

        public MacOSMountedVolumeInfoListener(ObservableCollection<MountedVolumeInfo> mountedDrives)
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
            var mountVolInfos = Directory.GetDirectories("/Volumes/")
                                .Where(p=> p != null)
                                .Select(p => new MountedVolumeInfo()
                                {
                                    VolumeLabel = Path.GetFileName(p),
                                    VolumePath = p,
                                    VolumeSizeBytes = 0
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

    public class MacOSMountedVolumeInfoProvider : IMountedVolumeInfoProvider
    {
        public IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            Contract.Requires<ArgumentNullException>(mountedDrives != null);
            return new MacOSMountedVolumeInfoListener(mountedDrives);
        }
    }
}
