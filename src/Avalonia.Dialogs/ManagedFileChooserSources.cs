using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform;
using Avalonia.Threading;

namespace Avalonia.Dialogs
{
    internal class ManagedFileChooserSources
    {
        public Func<ManagedFileChooserNavigationItem[]> GetUserDirectories { get; set; }
            = DefaultGetUserDirectories;

        public Func<ManagedFileChooserNavigationItem[]> GetFileSystemRoots { get; set; }
            = DefaultGetFileSystemRoots;

        public Func<ManagedFileChooserSources, ManagedFileChooserNavigationItem[]> GetAllItemsDelegate { get; set; }
            = DefaultGetAllItems;

        public ManagedFileChooserNavigationItem[] GetAllItems() => GetAllItemsDelegate(this);

        public static ManagedFileChooserNavigationItem[] DefaultGetAllItems(ManagedFileChooserSources sources)
        {
            return sources.GetUserDirectories().Concat(sources.GetFileSystemRoots()).ToArray();
        }

        private static Environment.SpecialFolder[] s_folders = new[]
        {
            Environment.SpecialFolder.Desktop,
            Environment.SpecialFolder.UserProfile,
            Environment.SpecialFolder.MyDocuments,
            Environment.SpecialFolder.MyMusic,
            Environment.SpecialFolder.MyPictures,
            Environment.SpecialFolder.MyVideos
        };

        private static string[] s_sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        private const string formatTemplate = "{0}{1:0.#} {2}";

        public static ManagedFileChooserNavigationItem[] DefaultGetUserDirectories()
        {
            return s_folders.Select(Environment.GetFolderPath).Distinct()
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Where(Directory.Exists)
                .Select(d => new ManagedFileChooserNavigationItem
                {
                    Path = d,
                    DisplayName = Path.GetFileName(d)
                }).ToArray();
        }

        public static event EventHandler SourcesChanged;

        private static string ByteToHumanReadableUnits(ulong size)
        {

            if (size == 0)
            {
                return string.Format(formatTemplate, null, 0, s_sizeSuffixes[0]);
            }

            var absSize = Math.Abs((double)size);
            var fpPower = Math.Log(absSize, 1000);
            var intPower = (int)fpPower;
            var iUnit = intPower >= s_sizeSuffixes.Length
                ? s_sizeSuffixes.Length - 1
                : intPower;
            var normSize = absSize / Math.Pow(1000, iUnit);

            return string.Format(
                formatTemplate,
                size < 0 ? "-" : null, normSize, s_sizeSuffixes[iUnit]);
        }

        public static ManagedFileChooserNavigationItem[] DefaultGetFileSystemRoots()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return DriveInfo.GetDrives().Select(d => new ManagedFileChooserNavigationItem
                {
                    DisplayName = d.Name,
                    Path = d.RootDirectory.FullName
                }).ToArray();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var paths = Directory.GetDirectories("/Volumes");

                return paths.Select(x => new ManagedFileChooserNavigationItem
                {
                    DisplayName = Path.GetFileName(x),
                    Path = x
                }).ToArray();
            }
            else
            {
                var drivesInfos = AvaloniaLocator.CurrentMutable
                                .GetService<IMountedDriveInfoProvider>()
                                .CurrentDrives;

                return drivesInfos
                       .Where(x => !x.MountPath.StartsWith("/boot"))
                       .Select(x =>
                       {
                           if (x.MountPath == "/")
                           {
                               return new ManagedFileChooserNavigationItem
                               {
                                   DisplayName = "File System",
                                   Path = "/"
                               };
                           }
                           else
                           {
                               var dNameEmpty = string.IsNullOrEmpty(x.DriveLabel.Trim());

                               return new ManagedFileChooserNavigationItem
                               {

                                   DisplayName = dNameEmpty ? $"{ByteToHumanReadableUnits(x.DriveSizeBytes)} Volume"
                                                            : x.DriveLabel,
                                   Path = x.MountPath
                               };
                           }
                       })
                       .ToArray();
            }
        }
    }
}
