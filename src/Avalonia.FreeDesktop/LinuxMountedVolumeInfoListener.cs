using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls.Platform;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Text;

namespace Avalonia.FreeDesktop
{
    internal class LinuxMountedVolumeInfoListener : IDisposable
    {
        private const string DevByLabelDir = "/dev/disk/by-label/";
        private const string ProcPartitionsDir = "/proc/partitions";
        private const string ProcMountsDir = "/proc/mounts";
        private CompositeDisposable _disposables;
        private ObservableCollection<MountedVolumeInfo> _targetObs;
        private bool _beenDisposed = false;

        public LinuxMountedVolumeInfoListener(ref ObservableCollection<MountedVolumeInfo> target)
        {
            _disposables = new CompositeDisposable();
            this._targetObs = target;

            var pollTimer = Observable.Interval(TimeSpan.FromSeconds(1))
                                      .Subscribe(Poll);

            _disposables.Add(pollTimer);

            Poll(0);
        }

        private string GetSymlinkTarget(string x) => Path.GetFullPath(Path.Combine(DevByLabelDir, NativeMethods.ReadLink(x)));

        private void Poll(long _)
        {
            var fProcPartitions = File.ReadAllLines(ProcPartitionsDir)
                                      .Skip(1)
                                      .Where(p => !string.IsNullOrEmpty(p))
                                      .Select(p => Regex.Replace(p, @"\s{2,}", " ").Trim().Split(' '))
                                      .Select(p => (p[2].Trim(), p[3].Trim()))
                                      .Select(p => (Convert.ToUInt64(p.Item1) * 1024, "/dev/" + p.Item2));

            var fProcMounts = File.ReadAllLines(ProcMountsDir)
                                  .Select(x => x.Split(' '))
                                  .Select(x => (x[0], x[1]));

            var labelDirEnum = Directory.Exists(DevByLabelDir) ?
                               new DirectoryInfo(DevByLabelDir).GetFiles() : Enumerable.Empty<FileInfo>();

            var labelDevPathPairs = labelDirEnum
                                    .Select(x => (GetSymlinkTarget(x.FullName), x.Name));

            var q1 = from mount in fProcMounts
                     join device in fProcPartitions on mount.Item1 equals device.Item2
                     join label in labelDevPathPairs on device.Item2 equals label.Item1 into labelMatches
                     from x in labelMatches.DefaultIfEmpty()
                     select new MountedVolumeInfo()
                     {
                         VolumePath = mount.Item2,
                         VolumeSizeBytes = device.Item1,
                         VolumeLabel = x.Name
                     };

            var mountVolInfos = q1.ToArray();

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
                    _disposables.Dispose();
                    _targetObs.Clear();
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
