using System;

namespace Avalonia.Dialogs.Internal
{
	class ManagedFileChooserItemViewModel : InternalViewModelBase
	{
		private string _displayName;
		private string _path;
		private bool _isDirectory;
		private DateTime _modified;
		private string _type;
		private long _size;

		public string DisplayName
		{
			get => _displayName;
			set => this.RaiseAndSetIfChanged(ref _displayName, value);
		}

		public string Path
		{
			get => _path;
			set => this.RaiseAndSetIfChanged(ref _path, value);
		}

		public DateTime Modified
		{
			get => _modified;
			set => this.RaiseAndSetIfChanged(ref _modified, value);
		}

		public string Type
		{
			get => _type;
			set => this.RaiseAndSetIfChanged(ref _type, value);
		}

		public long Size
		{
			get => _size;
			set => this.RaiseAndSetIfChanged(ref _size, value);
		}

		public string IconKey => IsDirectory ? "Icon_Folder" : "Icon_File";

		public bool IsDirectory
		{
			get => _isDirectory;
			set
			{
				if (this.RaiseAndSetIfChanged(ref _isDirectory, value))
				{
					this.RaisePropertyChanged(nameof(IconKey));
				}
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
