namespace Avalonia.Dialogs.Internal
{
    class ManagedFileChooserItemViewModel : InternalViewModelBase
    {
        private string _displayName;
        private string _path;
        private bool _isDirectory;

        public string DisplayName
        {
            get => _displayName;
            set => RaiseAndSetIfChanged(ref _displayName, value);
        }

        public string Path
        {
            get => _path;
            set => RaiseAndSetIfChanged(ref _path, value);
        }

        public string IconKey => IsDirectory ? "Icon_Folder" : "Icon_File";

        public bool IsDirectory
        {
            get => _isDirectory;
            set
            {
                if (RaiseAndSetIfChanged(ref _isDirectory, value))
                    RaisePropertyChanged(nameof(IconKey));
            }
        }

        public ManagedFileChooserItemViewModel()
        {
                
        }

        public ManagedFileChooserItemViewModel(ManagedFileChooserNavigationItem item)
        {
            IsDirectory = true;
            Path = item.Path;
            DisplayName = item.DisplayName;
        }
    }
}
