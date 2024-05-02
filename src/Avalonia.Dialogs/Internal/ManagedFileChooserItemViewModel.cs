using System;

namespace Avalonia.Dialogs.Internal
{
    public class ManagedFileChooserItemViewModel : AvaloniaDialogsInternalViewModelBase
    {
        private string? _displayName;
        private string? _path;
        private DateTime _modified;
        private string? _type;
        private long _size;
        private ManagedFileChooserItemType _itemType;

        public string? DisplayName
        {
            get => _displayName;
            set => this.RaiseAndSetIfChanged(ref _displayName, value);
        }

        public string? Path
        {
            get => _path;
            set => this.RaiseAndSetIfChanged(ref _path, value);
        }

        public DateTime Modified
        {
            get => _modified;
            set => this.RaiseAndSetIfChanged(ref _modified, value);
        }

        public string? Type
        {
            get => _type;
            set => this.RaiseAndSetIfChanged(ref _type, value);
        }

        public long Size
        {
            get => _size;
            set => this.RaiseAndSetIfChanged(ref _size, value);
        }

        public ManagedFileChooserItemType ItemType
        {
            get => _itemType;
            set => this.RaiseAndSetIfChanged(ref _itemType, value);
        }

        public string IconKey
        {
            get
            {
                switch (ItemType)
                {
                    case ManagedFileChooserItemType.Folder:
                        return "Icon_Folder";
                    case ManagedFileChooserItemType.Volume:
                        return "Icon_Volume";
                    default:
                        return "Icon_File";
                }
            }
        }
 
        public ManagedFileChooserItemViewModel()
        {
        }

        public ManagedFileChooserItemViewModel(ManagedFileChooserNavigationItem item)
        {
            ItemType = item.ItemType;
            Path = item.Path;
            DisplayName = item.DisplayName;
        }
    }
}
