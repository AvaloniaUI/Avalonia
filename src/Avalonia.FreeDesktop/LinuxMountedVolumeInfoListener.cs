using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Avalonia.Controls.Platform;
using Tmds.DBus;
using System.Reactive.Disposables;

namespace Avalonia.FreeDesktop.Dbus
{
    public partial class LinuxMountedVolumeInfoProvider
    {
        private class LinuxMountedVolumeInfoListener : IDisposable
        {
            private CompositeDisposable _disposables;
            private readonly Connection _sysDbus;
            private readonly IObjectManager _udisk2Manager;
            private readonly ObservableCollection<MountedVolumeInfo> _targetObs;
            private bool disposedValue = false;

            public LinuxMountedVolumeInfoListener(ref ObservableCollection<MountedVolumeInfo> target)
            {
                this._sysDbus = Connection.System;
                this._udisk2Manager = _sysDbus.CreateProxy<IObjectManager>("org.freedesktop.UDisks2", "/org/freedesktop/UDisks2");
                this._targetObs = target;
                Start();
            }

            private async void Poll()
            {
                var newDriveList = new List<MountedVolumeInfo>();

                var fProcMounts = File.ReadAllLines("/proc/mounts");

                var managedObj = await _udisk2Manager.GetManagedObjectsAsync();

                var res_drives = managedObj.Where(x => x.Key.ToString().Contains("/org/freedesktop/UDisks2/drives/"))
                                     .Select(x => x.Key);

                var res_blockdev = managedObj.Where(x => x.Key.ToString().Contains("/org/freedesktop/UDisks2/block_devices/"))
                                       .Select(x => x);

                var res_fs = managedObj.Where(x => x.Key.ToString().Contains("system"))
                                 .Select(x => x.Key)
                                 .ToList();

                foreach (var block in res_blockdev)
                {
                    try
                    {
                        var iblock = _sysDbus.CreateProxy<IBlock>("org.freedesktop.UDisks2", block.Key);
                        var iblockProps = await iblock.GetAllAsync();

                        var block_drive = await iblock.GetDriveAsync();
                        if (!res_drives.Contains(block_drive)) continue;

                        var drive_key = res_drives.Single(x => x == block_drive);
                        var drives = _sysDbus.CreateProxy<IDrive>("org.freedesktop.UDisks2", drive_key);
                        var drivesProps = await drives.GetAllAsync();

                        var devRawBytes = iblockProps.Device.Take(iblockProps.Device.Length - 1).ToArray();
                        var devPath = System.Text.Encoding.UTF8.GetString(devRawBytes);

                        var blockLabel = iblockProps.IdLabel;
                        var blockSize = iblockProps.Size;
                        var driveName = drivesProps.Id;

                        // HACK:    There should be something in udisks2 to 
                        //          get this data but I have no idea where.
                        var mountPoint = fProcMounts.Select(x => x.Split(' '))
                                                    .Where(x => x[0] == devPath)
                                                    .Select(x => x[1])
                                                    .SingleOrDefault();

                        if (mountPoint is null) continue;

                        var k = new MountedVolumeInfo()
                        {
                            VolumeLabel = blockLabel,
                            VolumeName = driveName,
                            VolumeSizeBytes = blockSize,
                            DevicePath = devPath,
                            MountPath = mountPoint
                        };

                        newDriveList.Add(k);
                    }
                    catch (Exception ex)
                    {
                        Logging.Logger.Warning("Linux Volume Listener", this, "Exception while enumerating DBus items: {0}; {1}"
                                               , ex.Message, ex.StackTrace);
                    }
                }

                UpdateCollection(_targetObs, newDriveList);
            }

            private async void Start()
            {
                _disposables = new CompositeDisposable();

                var sub1 = await _udisk2Manager.WatchInterfacesAddedAsync(delegate { Poll(); });
                var sub2 = await _udisk2Manager.WatchInterfacesRemovedAsync(delegate { Poll(); });

                _disposables.Add(sub1);
                _disposables.Add(sub2);

                Poll();
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        _disposables.Dispose();
                        _targetObs.Clear();
                    }

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            // https://stackoverflow.com/questions/19558644/update-an-observablecollection-from-another-collection
            private void UpdateCollection(ObservableCollection<MountedVolumeInfo> target, IEnumerable<MountedVolumeInfo> newCollection)
            {
                var newCollectionEnumerator = newCollection.GetEnumerator();
                var collectionEnumerator = target.GetEnumerator();

                var itemsToDelete = new Collection<MountedVolumeInfo>();
                while (collectionEnumerator.MoveNext())
                {
                    var item = collectionEnumerator.Current;

                    if (!newCollection.Contains(item))
                        itemsToDelete.Add(item);
                }

                foreach (var itemToDelete in itemsToDelete)
                {
                    target.Remove(itemToDelete);
                }

                var i = 0;

                while (newCollectionEnumerator.MoveNext())
                {
                    var item = newCollectionEnumerator.Current;

                    if (!target.Contains(item))
                    {
                        target.Insert(i, item);
                    }
                    else
                    {
                        int oldIndex = target.IndexOf(item);
                        target.Move(oldIndex, i);
                    }
                    i++;
                }
            }
        }
    }
}
