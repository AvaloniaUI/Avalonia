using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Reactive;
using Avalonia.Controls.Platform;
using Avalonia.Threading;

namespace Avalonia.Native
{
    internal class MacOSMountedVolumeInfoListener : IDisposable
    {
        private readonly IDisposable _disposable;
        private bool _beenDisposed = false;
        private ObservableCollection<MountedVolumeInfo> mountedDrives;

        public MacOSMountedVolumeInfoListener(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            this.mountedDrives = mountedDrives;

            _disposable = DispatcherTimer.Run(Poll, TimeSpan.FromSeconds(1));

            Poll();
        }

        private bool Poll()
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
                return true;
            else
            {
                mountedDrives.Clear();

                foreach (var i in mountVolInfos)
                    mountedDrives.Add(i);
                return true;
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

    internal class MacOSMountedVolumeInfoProvider : IMountedVolumeInfoProvider
    {
        public IDisposable Listen(ObservableCollection<MountedVolumeInfo> mountedDrives)
        {
            return new MacOSMountedVolumeInfoListener(mountedDrives);
        }
    }
}
