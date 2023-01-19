using System;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using Avalonia.Controls.Platform;
using Avalonia.Reactive;
using System.Text.RegularExpressions;
using Avalonia.Threading;

namespace Avalonia.FreeDesktop
{
    internal class LinuxMountedVolumeInfoListener : IDisposable
    {
        private const string DevByLabelDir = "/dev/disk/by-label/";
        private const string ProcPartitionsDir = "/proc/partitions";
        private const string ProcMountsDir = "/proc/mounts";
        private IDisposable _disposable;
        private ObservableCollection<MountedVolumeInfo> _targetObs;
        private bool _beenDisposed = false;

        public LinuxMountedVolumeInfoListener(ref ObservableCollection<MountedVolumeInfo> target)
        {
            this._targetObs = target;

            _disposable = DispatcherTimer.Run(Poll, TimeSpan.FromSeconds(1));

            Poll();
        }

        private static string GetSymlinkTarget(string x) => Path.GetFullPath(Path.Combine(DevByLabelDir, NativeMethods.ReadLink(x)));

        private static string UnescapeString(string input, string regexText, int escapeBase) =>
            new Regex(regexText).Replace(input, m => Convert.ToChar(Convert.ToByte(m.Groups[1].Value, escapeBase)).ToString());

        private static string UnescapePathFromProcMounts(string input) => UnescapeString(input, @"\\(\d{3})", 8);

        private static string UnescapeDeviceLabel(string input) => UnescapeString(input, @"\\x([0-9a-f]{2})", 16);

        private bool Poll()
        {
            var fProcPartitions = File.ReadAllLines(ProcPartitionsDir)
                                      .Skip(1)
                                      .Where(p => !string.IsNullOrEmpty(p))
                                      .Select(p => Regex.Replace(p, @"\s{2,}", " ").Trim().Split(' '))
                                      .Select(p => (p[2].Trim(), p[3].Trim()))
                                      .Select(p => (Convert.ToUInt64(p.Item1) * 1024, "/dev/" + p.Item2));

            var fProcMounts = File.ReadAllLines(ProcMountsDir)
                                  .Select(x => x.Split(' '))
                                  .Select(x => (x[0], UnescapePathFromProcMounts(x[1])))
                                  .Where(x => !x.Item2.StartsWith("/snap/", StringComparison.InvariantCultureIgnoreCase));

            var labelDirEnum = Directory.Exists(DevByLabelDir) ?
                               new DirectoryInfo(DevByLabelDir).GetFiles() : Enumerable.Empty<FileInfo>();

            var labelDevPathPairs = labelDirEnum
                                    .Select(x => (GetSymlinkTarget(x.FullName), UnescapeDeviceLabel(x.Name)));

            var q1 = from mount in fProcMounts
                     join device in fProcPartitions on mount.Item1 equals device.Item2
                     join label in labelDevPathPairs on device.Item2 equals label.Item1 into labelMatches
                     from x in labelMatches.DefaultIfEmpty()
                     select new MountedVolumeInfo()
                     {
                         VolumePath = mount.Item2,
                         VolumeSizeBytes = device.Item1,
                         VolumeLabel = x.Item2
                     };

            var mountVolInfos = q1.ToArray();

            if (_targetObs.SequenceEqual(mountVolInfos))
                return true;
            else
            {
                _targetObs.Clear();

                foreach (var i in mountVolInfos)
                    _targetObs.Add(i);
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
