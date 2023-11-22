using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Avalonia.Reactive;
using System.Runtime.InteropServices;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Avalonia.Dialogs.Internal
{
    public class ManagedFileChooserViewModel : AvaloniaDialogsInternalViewModelBase
    {
        private readonly ManagedFileDialogOptions _options;
        public event Action? CancelRequested;
        public event Action<string[]>? CompleteRequested;
        public event Action<string>? OverwritePrompt;

        public AvaloniaList<ManagedFileChooserItemViewModel> QuickLinks { get; } =
            new AvaloniaList<ManagedFileChooserItemViewModel>();

        public AvaloniaList<ManagedFileChooserItemViewModel> Items { get; } =
            new AvaloniaList<ManagedFileChooserItemViewModel>();

        public AvaloniaList<ManagedFileChooserFilterViewModel> Filters { get; } =
            new AvaloniaList<ManagedFileChooserFilterViewModel>();

        public AvaloniaList<ManagedFileChooserItemViewModel> SelectedItems { get; } =
            new AvaloniaList<ManagedFileChooserItemViewModel>();

        string? _location;
        string? _fileName;
        private bool _showHiddenFiles;
        private ManagedFileChooserFilterViewModel? _selectedFilter;
        private readonly bool _selectingDirectory;
        private readonly bool _savingFile;
        private bool _scheduledSelectionValidation;
        private bool _alreadyCancelled = false;
        private string? _defaultExtension;
        private readonly bool _overwritePrompt;
        private CompositeDisposable _disposables;

        public string? Location
        {
            get => _location;
            set => this.RaiseAndSetIfChanged(ref _location, value);
        }

        public string? FileName
        {
            get => _fileName;
            set => this.RaiseAndSetIfChanged(ref _fileName, value);
        }

        public bool SelectingFolder => _selectingDirectory;

        public bool ShowFilters { get; }
        public SelectionMode SelectionMode { get; }
        public string? Title { get; }

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

        public ManagedFileChooserFilterViewModel? SelectedFilter
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

        private void RefreshQuickLinks(ManagedFileChooserSources quickSources)
        {
            QuickLinks.Clear();
            QuickLinks.AddRange(quickSources.GetAllItems().Select(i => new ManagedFileChooserItemViewModel(i)));
        }

        public ManagedFileChooserViewModel(ManagedFileDialogOptions options)
        {
            _options = options;
            _disposables = new CompositeDisposable();

            var quickSources = AvaloniaLocator.Current
                                              .GetService<ManagedFileChooserSources>()
                                              ?? new ManagedFileChooserSources();

            var sub1 = (AvaloniaLocator.Current.GetService<IMountedVolumeInfoProvider>()
                        ?? new BclMountedVolumeInfoProvider())
                .Listen(ManagedFileChooserSources.MountedVolumes);

            var sub2 = ManagedFileChooserSources.MountedVolumes.GetWeakCollectionChangedObservable()
                .Subscribe(x => Dispatcher.UIThread.Post(() => RefreshQuickLinks(quickSources)));

            _disposables.Add(sub1);
            _disposables.Add(sub2);

            CompleteRequested += delegate { _disposables?.Dispose(); };
            CancelRequested += delegate { _disposables?.Dispose(); };

            RefreshQuickLinks(quickSources);
            
            SelectedItems.CollectionChanged += OnSelectionChangedAsync;
        }

        public ManagedFileChooserViewModel(FilePickerOpenOptions filePickerOpen,  ManagedFileDialogOptions options)
            : this(options)
        {
            Title = filePickerOpen.Title ?? "Open file";
            
            if (filePickerOpen.FileTypeFilter?.Count > 0)
            {
                Filters.AddRange(filePickerOpen.FileTypeFilter.Select(f => new ManagedFileChooserFilterViewModel(f)));
                _selectedFilter = Filters[0];
                ShowFilters = true;
            }

            if (filePickerOpen.AllowMultiple)
            {
                SelectionMode = SelectionMode.Multiple;
            }

            Navigate(filePickerOpen.SuggestedStartLocation);
        }
        
        public ManagedFileChooserViewModel(FilePickerSaveOptions filePickerSave,  ManagedFileDialogOptions options)
            : this(options)
        {
            Title = filePickerSave.Title ?? "Save file";
            
            if (filePickerSave.FileTypeChoices?.Count > 0)
            {
                Filters.AddRange(filePickerSave.FileTypeChoices.Select(f => new ManagedFileChooserFilterViewModel(f)));
                _selectedFilter = Filters[0];
                ShowFilters = true;
            }
            
            _savingFile = true;
            _defaultExtension = filePickerSave.DefaultExtension;
            _overwritePrompt = filePickerSave.ShowOverwritePrompt ?? true;
            FileName = filePickerSave.SuggestedFileName;
            
            Navigate(filePickerSave.SuggestedStartLocation, FileName);
        }
        
        public ManagedFileChooserViewModel(FolderPickerOpenOptions folderPickerOpen,  ManagedFileDialogOptions options)
            : this(options)
        {
            Title = folderPickerOpen.Title ?? "Select directory";

            _selectingDirectory = true;

            if (folderPickerOpen.AllowMultiple)
            {
                SelectionMode = SelectionMode.Multiple;
            }

            Navigate(folderPickerOpen.SuggestedStartLocation);
        }

        public void EnterPressed()
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

        private async void OnSelectionChangedAsync(object? sender, NotifyCollectionChangedEventArgs e)
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
                        if (!_options.AllowDirectorySelection)
                        {
                            var invalidItems = SelectedItems.Where(i => i.ItemType == ManagedFileChooserItemType.Folder)
                                .ToList();
                            foreach (var item in invalidItems) 
                                SelectedItems.Remove(item);
                        }

                        if (!_selectingDirectory)
                        {
                            var selectedItem = SelectedItems.FirstOrDefault();
                            
						    if (selectedItem != null)
						    {
						        FileName = selectedItem.DisplayName;
						    }
                        }
                    }
                }
                finally
                {
                    _scheduledSelectionValidation = false;
                }
            });
        }

        void NavigateRoot(string? initialSelectionName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Navigate(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System))!, initialSelectionName);
            }
            else
            {
                Navigate("/", initialSelectionName);
            }
        }

        public void Refresh() => Navigate(Location);

        public void Navigate(IStorageFolder? path, string? initialSelectionName = null)
        {
            var fullDirectoryPath = path?.TryGetLocalPath() ?? Directory.GetCurrentDirectory();
            
            Navigate(fullDirectoryPath, initialSelectionName);
        }
        
        public void Navigate(string? path, string? initialSelectionName = null)
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
                            infos = infos.Where(i => (i.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0);
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
                    })
                    .Where(x => x.Exists)
                    .Select(info => new ManagedFileChooserItemViewModel
                    {
                        DisplayName = info.Name,
                        Path = info.FullName,
                        Type = info is FileInfo ? info.Extension : "File Folder",
                        ItemType = info is FileInfo ? ManagedFileChooserItemType.File
                                                     : ManagedFileChooserItemType.Folder,
                        Size = info is FileInfo f ? f.Length : 0,
                        Modified = info.LastWriteTime
                    })
                    .OrderByDescending(x => x.ItemType == ManagedFileChooserItemType.Folder)
                    .ThenBy(x => x.DisplayName, StringComparer.InvariantCultureIgnoreCase));

                    if (initialSelectionName != null)
                    {
                        var sel = Items.FirstOrDefault(i => i.ItemType == ManagedFileChooserItemType.File && i.DisplayName == initialSelectionName);

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
            if (!_alreadyCancelled)
            {
                // INFO: Don't misplace this check or it might cause
                //       StackOverflowException because of recursive
                //       event invokes.
                _alreadyCancelled = true;
                CancelRequested?.Invoke();
            }
        }

        public void Ok()
        {
            if (_selectingDirectory)
            {
                CompleteRequested?.Invoke(new[] { Location! });
            }
            else if (_savingFile)
            {
                if (!string.IsNullOrWhiteSpace(FileName))
                {
                    if (!Path.HasExtension(FileName) && !string.IsNullOrWhiteSpace(_defaultExtension))
                    {
                        FileName = Path.ChangeExtension(FileName, _defaultExtension);
                    }

                    var fullName = Path.Combine(Location!, FileName);

                    if (_overwritePrompt && File.Exists(fullName))
                    {
                        OverwritePrompt?.Invoke(fullName);
                    }
                    else
                    {
                        CompleteRequested?.Invoke(new[] { fullName });
                    }
                }
            }
            else
            {
                CompleteRequested?.Invoke(SelectedItems.Select(i => i.Path!).ToArray());
            }
        }

        public void SelectSingleFile(ManagedFileChooserItemViewModel item)
        {
            CompleteRequested?.Invoke(new[] { item.Path! });
        }
    }
}
