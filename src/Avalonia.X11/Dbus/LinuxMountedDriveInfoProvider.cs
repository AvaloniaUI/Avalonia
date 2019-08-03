using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Avalonia.Controls.Platform;
using Tmds.DBus;

namespace Avalonia.X11.Dbus
{
    public class LinuxMountedDriveInfoProvider : IMountedDriveInfoProvider
    {
        private IDisposable[] Disposables;
        private readonly Connection _sysDbus;
        private readonly IObjectManager udisk2Manager;

        public LinuxMountedDriveInfoProvider()
        {
            this._sysDbus = Connection.System;

            this.udisk2Manager = _sysDbus.CreateProxy<IObjectManager>("org.freedesktop.UDisks2", "/org/freedesktop/UDisks2");

            Start();
        }

        async void Start()
        {
            Disposables = new[] {
                await udisk2Manager.WatchInterfacesAddedAsync(delegate { Poll(); }),
                await udisk2Manager.WatchInterfacesRemovedAsync( delegate { Poll(); })
            };

            Poll();
        }
        
        public ObservableCollection<MountedDriveInfo> MountedDrives { get; } = new ObservableCollection<MountedDriveInfo>();
        private async void Poll()
        {
            var newDriveList = new List<MountedDriveInfo>();

            var fProcMounts = File.ReadAllLines("/proc/mounts");

            var managedObj = await udisk2Manager.GetManagedObjectsAsync();

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

                    // There should be something in udisks2 to 
                    // get this data but I have no idea where.
                    var mountPoint = fProcMounts.Select(x => x.Split(' '))
                                                .Where(x => x[0] == devPath)
                                                .Select(x => x[1])
                                                .SingleOrDefault();

                    if (mountPoint is null) continue;

                    var k = new MountedDriveInfo()
                    {
                        DriveLabel = blockLabel,
                        DriveName = driveName,
                        DriveSizeBytes = blockSize,
                        DevicePath = devPath,
                        MountPath = mountPoint
                    };

                    newDriveList.Add(k);
                }
                finally
                {

                }
            }

            UpdateCollection(newDriveList);
        }

        // https://stackoverflow.com/questions/19558644/update-an-observablecollection-from-another-collection
        private void UpdateCollection(IEnumerable<MountedDriveInfo> newCollection)
        {
            var newCollectionEnumerator = newCollection.GetEnumerator();
            var collectionEnumerator = MountedDrives.GetEnumerator();

            var itemsToDelete = new Collection<MountedDriveInfo>();
            while (collectionEnumerator.MoveNext())
            {
                var item = collectionEnumerator.Current;

                // Store item to delete (we can't do it while parse collection.
                if (!newCollection.Contains(item))
                {
                    itemsToDelete.Add(item);
                }
            }

            // Handle item to delete.
            foreach (var itemToDelete in itemsToDelete)
            {
                MountedDrives.Remove(itemToDelete);
            }

            var i = 0;
            while (newCollectionEnumerator.MoveNext())
            {
                var item = newCollectionEnumerator.Current;

                // Handle new item.
                if (!MountedDrives.Contains(item))
                {
                    MountedDrives.Insert(i, item);
                }

                // Handle existing item, move at the good index.
                if (MountedDrives.Contains(item))
                {
                    int oldIndex = MountedDrives.IndexOf(item);
                    MountedDrives.Move(oldIndex, i);
                }
                i++;
            }
        }


        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var Disposable in Disposables)
                        Disposable.Dispose();

                    MountedDrives?.Clear();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
    }
}
