using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.InteropServices;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;

namespace Avalonia.Dialogs.Internal
{
    class ManagedFileChooserViewModel : InternalViewModelBase
	{
		public event Action CancelRequested;
		public event Action<string[]> CompleteRequested;

		public AvaloniaList<ManagedFileChooserItemViewModel> QuickLinks { get; } =
			new AvaloniaList<ManagedFileChooserItemViewModel>();

		public AvaloniaList<ManagedFileChooserItemViewModel> Items { get; } =
			new AvaloniaList<ManagedFileChooserItemViewModel>();

		public AvaloniaList<ManagedFileChooserFilterViewModel> Filters { get; } =
			new AvaloniaList<ManagedFileChooserFilterViewModel>();

		public AvaloniaList<ManagedFileChooserItemViewModel> SelectedItems { get; } =
			new AvaloniaList<ManagedFileChooserItemViewModel>();

		string _location;
		string _fileName;
		private bool _showHiddenFiles;
		private ManagedFileChooserFilterViewModel _selectedFilter;
		private bool _selectingDirectory;
		private bool _savingFile;
		private bool _scheduledSelectionValidation;
		private string _defaultExtension;

		public string Location
		{
			get => _location;
			private set => this.RaiseAndSetIfChanged(ref _location, value);
		}

		public string FileName
		{
			get => _fileName;
			private set => this.RaiseAndSetIfChanged(ref _fileName, value);
		}

		public bool SelectingFolder => _selectingDirectory;

		public bool ShowFilters { get; }
		public SelectionMode SelectionMode { get; }
		public string Title { get; }

		public int QuickLinksSelectedIndex
		{
			get
			{
				for (var index = 0; index < QuickLinks.Count; index++)
				{
					var i = QuickLinks[index];

					if (i.Path == Location)
					{
						return index;
					}
				}

				return -1;
			}
			set => this.RaisePropertyChanged(nameof(QuickLinksSelectedIndex));
		}

		public ManagedFileChooserFilterViewModel SelectedFilter
		{
			get => _selectedFilter;
			set
			{
				this.RaiseAndSetIfChanged(ref _selectedFilter, value);
				Refresh();
			}
		}

		public bool ShowHiddenFiles
		{
			get => _showHiddenFiles;
			set
			{
				this.RaiseAndSetIfChanged(ref _showHiddenFiles, value);
				Refresh();
			}
		}

		public ManagedFileChooserViewModel(FileSystemDialog dialog)
		{
			var quickSources = AvaloniaLocator.Current.GetService<ManagedFileChooserSources>()
						   ?? new ManagedFileChooserSources();

			QuickLinks.Clear();

			QuickLinks.AddRange(quickSources.GetAllItems().Select(i => new ManagedFileChooserItemViewModel(i)));

			Title = dialog.Title ?? (
						dialog is OpenFileDialog ? "Open file"
						: dialog is SaveFileDialog ? "Save file"
						: dialog is OpenFolderDialog ? "Select directory"
						: throw new ArgumentException(nameof(dialog)));

			var directory = dialog.InitialDirectory;

			if (directory == null || !Directory.Exists(directory))
			{
				directory = Directory.GetCurrentDirectory();
			}

			if (dialog is FileDialog fd)
			{
				if (fd.Filters?.Count > 0)
				{
					Filters.AddRange(fd.Filters.Select(f => new ManagedFileChooserFilterViewModel(f)));
					_selectedFilter = Filters[0];
					ShowFilters = true;
				}

				if (dialog is OpenFileDialog ofd)
				{
					if (ofd.AllowMultiple)
					{
						SelectionMode = SelectionMode.Multiple;
					}
				}
			}

			_selectingDirectory = dialog is OpenFolderDialog;			

			if(dialog is SaveFileDialog sfd)
			{
				_savingFile = true;
				_defaultExtension = sfd.DefaultExtension;
				FileName = sfd.InitialFileName;
			}

			Navigate(directory, (dialog as FileDialog)?.InitialFileName);
			SelectedItems.CollectionChanged += OnSelectionChangedAsync;
		}

        public void EnterPressed ()
        {
            if (Directory.Exists(Location))
            {
                Navigate(Location);
            }
            else if (File.Exists(Location))
            {
                CompleteRequested?.Invoke(new[] { Location });
            }
        }

		private async void OnSelectionChangedAsync(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_scheduledSelectionValidation)
			{
				return;
			}

			_scheduledSelectionValidation = true;
			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				try
				{
					if (_selectingDirectory)
					{
						SelectedItems.Clear();
					}
					else
					{
						var invalidItems = SelectedItems.Where(i => i.IsDirectory).ToList();
						foreach (var item in invalidItems)
						{
							SelectedItems.Remove(item);
						}

						if(!_selectingDirectory)
						{
							FileName = SelectedItems.FirstOrDefault()?.DisplayName;
						}
					}
				}
				finally
				{
					_scheduledSelectionValidation = false;
				}
			});
		}

		void NavigateRoot(string initialSelectionName)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Navigate(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), initialSelectionName);
			}
			else
			{
				Navigate("/", initialSelectionName);
			}
		}

		public void Refresh() => Navigate(Location);

		public void Navigate(string path, string initialSelectionName = null)
		{
			if (!Directory.Exists(path))
			{
				NavigateRoot(initialSelectionName);
			}
			else
			{
				Location = path;
				Items.Clear();
				SelectedItems.Clear();

				try
				{
					var infos = new DirectoryInfo(path).EnumerateFileSystemInfos();

					if (!ShowHiddenFiles)
					{
						if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
						{
							infos = infos.Where(i => (i.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0);
						}
						else
						{
							infos = infos.Where(i => !i.Name.StartsWith("."));
						}
					}

					if (SelectedFilter != null)
					{
						infos = infos.Where(i => i is DirectoryInfo || SelectedFilter.Match(i.Name));
					}

					Items.AddRange(infos.Where(x =>
					{
						if (_selectingDirectory)
						{
							if (!(x is DirectoryInfo))
							{
								return false;
							}
						}

						return true;
					}).Select(info => new ManagedFileChooserItemViewModel
					{
						DisplayName = info.Name,
						Path = info.FullName,
						IsDirectory = info is DirectoryInfo,
						Type = info is FileInfo ? info.Extension : "File Folder",
						Size = info is FileInfo f ? f.Length : 0,
						Modified = info.LastWriteTime
					})
					.OrderByDescending(x => x.IsDirectory)
					.ThenBy(x => x.DisplayName, StringComparer.InvariantCultureIgnoreCase));

					if (initialSelectionName != null)
					{
						var sel = Items.FirstOrDefault(i => !i.IsDirectory && i.DisplayName == initialSelectionName);

						if (sel != null)
						{
							SelectedItems.Add(sel);
						}
					}

					this.RaisePropertyChanged(nameof(QuickLinksSelectedIndex));
				}
				catch (System.UnauthorizedAccessException)
				{
				}
			}
		}

		public void GoUp()
		{
			var parent = Path.GetDirectoryName(Location);

			if (string.IsNullOrWhiteSpace(parent))
			{
				return;
			}

			Navigate(parent);
		}

		public void Cancel()
		{
			CancelRequested?.Invoke();
		}

		public void Ok()
		{
			if (_selectingDirectory)
			{
				CompleteRequested?.Invoke(new[] { Location });
			}
			else if(_savingFile)
			{
				if (!string.IsNullOrWhiteSpace(FileName))
				{
					if (!Path.HasExtension(FileName) && !string.IsNullOrWhiteSpace(_defaultExtension))
					{
						FileName = Path.ChangeExtension(FileName, _defaultExtension);
					}

					CompleteRequested?.Invoke(new[] { Path.Combine(Location, FileName) });
				}
			}
			else
			{
				CompleteRequested?.Invoke(SelectedItems.Select(i => i.Path).ToArray());
			}
		}

		public void SelectSingleFile(ManagedFileChooserItemViewModel item)
		{
			CompleteRequested?.Invoke(new[] { item.Path });
		}
	}
}
