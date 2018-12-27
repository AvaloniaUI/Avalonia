using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Avalonia.Dialogs.Internal
{
    public class ManagedFileChooserSources
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

            return new[]
            {
                new ManagedFileChooserNavigationItem
                {
                    DisplayName = "File System",
                    Path = "/"
                }
            };
        }
    }

    public class ManagedFileChooserNavigationItem
    {
        public string DisplayName { get; set; }
        public string Path { get; set; }
    }
}
