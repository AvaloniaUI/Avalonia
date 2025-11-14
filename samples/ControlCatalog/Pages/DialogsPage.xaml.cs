using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Dialogs;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

#pragma warning disable CS0618 // Type or member is obsolete
#nullable enable

namespace ControlCatalog.Pages
{
    public partial class DialogsPage : UserControl
    {
        public DialogsPage()
        {
            InitializeComponent();

            IStorageFolder? lastSelectedDirectory = null;
            IStorageItem? lastSelectedItem = null;
            bool ignoreTextChanged = false;

            var results = PickerLastResults;
            var resultsVisible = PickerLastResultsVisible;
            var bookmarkContainer = BookmarkContainer;
            var openedFileContent = OpenedFileContent;
            var openMultiple = OpenMultiple;
            var currentFolderBox = CurrentFolderBox;
            var useSuggestedFilter = UseSuggestedFilter;
            var suggestedFilterSelector = SuggestedFilterSelector;

            currentFolderBox.TextChanged += async (sender, args) =>
            {
                if (ignoreTextChanged) return;

                if (Enum.TryParse<WellKnownFolder>(currentFolderBox.Text, true, out var folderEnum))
                {
                    lastSelectedDirectory = await GetStorageProvider().TryGetWellKnownFolderAsync(folderEnum);
                }
                else if (!string.IsNullOrWhiteSpace(currentFolderBox.Text))
                {
                    if (!Uri.TryCreate(currentFolderBox.Text, UriKind.Absolute, out var folderLink))
                    {
                        Uri.TryCreate("file://" + currentFolderBox.Text, UriKind.Absolute, out folderLink);
                    }

                    if (folderLink is not null)
                    {
                        try
                        {
                            lastSelectedDirectory = await GetStorageProvider().TryGetFolderFromPathAsync(folderLink);
                        }
                        catch (SecurityException)
                        {
                        
                        }
                    }
                }
            };


            List<FileDialogFilter> GetFilters()
            {
                return GetFileTypes()?.Select(f => new FileDialogFilter
                {
                    Name = f.Name, Extensions = f.Patterns!.ToList()
                }).ToList() ?? new List<FileDialogFilter>();
            }

            List<FilePickerFileType>? BuildFileTypes()
            {
                var selectedItem = (FilterSelector.SelectedItem as ComboBoxItem)?.Content
                    ?? "None";

                var binLogType = new FilePickerFileType("Binary Log")
                {
                    Patterns = new[] { "*.binlog", "*.buildlog" },
                    MimeTypes = new[] { "application/binlog", "application/buildlog" },
                    AppleUniformTypeIdentifiers = new[] { "public.data" }
                };

                return selectedItem switch
                {
                    "All + TXT + BinLog" => new List<FilePickerFileType>
                    {
                        FilePickerFileTypes.All, FilePickerFileTypes.TextPlain, binLogType
                    },
                    "Binlog" => new List<FilePickerFileType> { binLogType },
                    "TXT extension only" => new List<FilePickerFileType>
                    {
                        new("TXT") { Patterns = FilePickerFileTypes.TextPlain.Patterns }
                    },
                    "TXT mime only" => new List<FilePickerFileType>
                    {
                        new("TXT") { MimeTypes = FilePickerFileTypes.TextPlain.MimeTypes }
                    },
                    "TXT apple type id only" => new List<FilePickerFileType>
                    {
                        new("TXT")
                        {
                            AppleUniformTypeIdentifiers =
                                FilePickerFileTypes.TextPlain.AppleUniformTypeIdentifiers
                        }
                    },
                    _ => null
                };
            }

            List<FilePickerFileType>? GetFileTypes()
            {
                var types = BuildFileTypes();
                UpdateSuggestedFilterSelector(types);
                return types;
            }

            void UpdateSuggestedFilterSelector(IReadOnlyList<FilePickerFileType>? types)
            {
                var previouslySelected = (suggestedFilterSelector.SelectedItem as ComboBoxItem)?.Tag as FilePickerFileType;
                suggestedFilterSelector.Items.Clear();
                suggestedFilterSelector.Items.Add(new ComboBoxItem { Content = "First filter", Tag = null });

                var desiredIndex = 0;
                if (types is { Count: > 0 })
                {
                    for (var i = 0; i < types.Count; i++)
                    {
                        var type = types[i];
                        var item = new ComboBoxItem { Content = type.Name, Tag = type };
                        suggestedFilterSelector.Items.Add(item);

                        if (previouslySelected is not null && ReferenceEquals(previouslySelected, type))
                        {
                            desiredIndex = i + 1;
                        }
                    }
                }

                suggestedFilterSelector.SelectedIndex = desiredIndex;
            }

            FilePickerFileType? GetSuggestedFileType(IReadOnlyList<FilePickerFileType>? types)
            {
                if (useSuggestedFilter.IsChecked == true && types is { Count: > 0 })
                {
                    if (suggestedFilterSelector.SelectedItem is ComboBoxItem { Tag: FilePickerFileType selectedType }
                        && types.Any(t => ReferenceEquals(t, selectedType)))
                    {
                        return selectedType;
                    }

                    return types.FirstOrDefault();
                }

                return null;
            }

            void UpdateSuggestedFilterSelectorState() =>
                suggestedFilterSelector.IsEnabled = useSuggestedFilter.IsChecked == true;

            useSuggestedFilter.Checked += (_, _) => UpdateSuggestedFilterSelectorState();
            useSuggestedFilter.Unchecked += (_, _) => UpdateSuggestedFilterSelectorState();
            UpdateSuggestedFilterSelectorState();

            FilterSelector.SelectionChanged += (_, _) => UpdateSuggestedFilterSelector(BuildFileTypes());
            UpdateSuggestedFilterSelector(BuildFileTypes());

            OpenFile.Click += async delegate
            {
                // Almost guaranteed to exist
                var uri = Assembly.GetEntryAssembly()?.GetModules().FirstOrDefault()?.FullyQualifiedName;
                var initialFileName = uri == null ? null : System.IO.Path.GetFileName(uri);
                var initialDirectory = uri == null ? null : System.IO.Path.GetDirectoryName(uri);

                var result = await new OpenFileDialog()
                {
                    Title = "Open file",
                    Filters = GetFilters(),
                    Directory = initialDirectory,
                    InitialFileName = initialFileName
                }.ShowAsync(GetWindow());
                results.ItemsSource = result;
                resultsVisible.IsVisible = result?.Any() == true;
            };
            OpenMultipleFiles.Click += async delegate
            {
                var result = await new OpenFileDialog()
                {
                    Title = "Open multiple files",
                    Filters = GetFilters(),
                    Directory = lastSelectedDirectory?.Path is {IsAbsoluteUri:true} path ? path.LocalPath : null,
                    AllowMultiple = true
                }.ShowAsync(GetWindow());
                results.ItemsSource = result;
                resultsVisible.IsVisible = result?.Any() == true;
            };
            SaveFile.Click += async delegate
            {
                var filters = GetFilters();
                var result = await new SaveFileDialog()
                {
                    Title = "Save file",
                    Filters = filters,
                    Directory = lastSelectedDirectory?.Path is {IsAbsoluteUri:true} path ? path.LocalPath : null,
                    DefaultExtension = filters?.Any() == true ? "txt" : null,
                    InitialFileName = "test.txt"
                }.ShowAsync(GetWindow());
                results.ItemsSource = new[] { result };
                resultsVisible.IsVisible = result != null;
            };
            SelectFolder.Click += async delegate
            {
                var result = await new OpenFolderDialog()
                {
                    Title = "Select folder",
                    Directory = lastSelectedDirectory?.Path is {IsAbsoluteUri:true} path ? path.LocalPath : null,
                }.ShowAsync(GetWindow());
                if (string.IsNullOrEmpty(result))
                {
                    resultsVisible.IsVisible = false;
                }
                else
                {
                    SetFolder(await GetStorageProvider().TryGetFolderFromPathAsync(result!));
                    results.ItemsSource = new[] { result };
                    resultsVisible.IsVisible = true;
                }
            };
            OpenBoth.Click += async delegate
            {
                var result = await new OpenFileDialog()
                {
                    Title = "Select both",
                    Directory = lastSelectedDirectory?.Path is {IsAbsoluteUri:true} path ? path.LocalPath : null,
                    AllowMultiple = true
                }.ShowManagedAsync(GetWindow(), new ManagedFileDialogOptions
                {
                    AllowDirectorySelection = true
                });
                results.ItemsSource = result;
                resultsVisible.IsVisible = result?.Any() == true;
            };
            DecoratedWindow.Click += delegate
            {
                new DecoratedWindow().Show();
            };
            DecoratedWindowDialog.Click += delegate
            {
                _ = new DecoratedWindow().ShowDialog(GetWindow());
            };
            Dialog.Click += delegate
            {
                var window = CreateSampleWindow();
                window.Height = 200;
                _ = window.ShowDialog(GetWindow());
            };
            DialogNoTaskbar.Click += delegate
            {
                var window = CreateSampleWindow();
                window.Height = 200;
                window.ShowInTaskbar = false;
                _ = window.ShowDialog(GetWindow());
            };
            OwnedWindow.Click += delegate
            {
                var window = CreateSampleWindow();

                window.Show(GetWindow());
            };

            OwnedWindowNoTaskbar.Click += delegate
            {
                var window = CreateSampleWindow();

                window.ShowInTaskbar = false;

                window.Show(GetWindow());
            };

            OpenFilePicker.Click += async delegate
            {
                var fileTypes = GetFileTypes();
                var result = await GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    Title = "Open file",
                    FileTypeFilter = fileTypes,
                    SuggestedFileType = GetSuggestedFileType(fileTypes),
                    SuggestedFileName = "FileName",
                    SuggestedStartLocation = lastSelectedDirectory,
                    AllowMultiple = openMultiple.IsChecked == true
                });

                await SetPickerResult(result);
            };
            SaveFilePicker.Click += async delegate
            {
                var fileTypes = GetFileTypes();
                var suggestedType = GetSuggestedFileType(fileTypes);
                var file = await GetStorageProvider().SaveFilePickerAsync(new FilePickerSaveOptions()
                {
                    Title = "Save file",
                    FileTypeChoices = fileTypes,
                    SuggestedFileType = suggestedType,
                    SuggestedStartLocation = lastSelectedDirectory,
                    SuggestedFileName = "FileName",
                    ShowOverwritePrompt = true
                });

                if (file is not null)
                {
                    try
                    {
                        // Sync disposal of StreamWriter is not supported on WASM
#if NET6_0_OR_GREATER
                        await using var stream = await file.OpenWriteAsync();
                        await using var writer = new System.IO.StreamWriter(stream);
#else
                        using var stream = await file.OpenWriteAsync();
                        using var writer = new System.IO.StreamWriter(stream);
#endif
                        await writer.WriteLineAsync(openedFileContent.Text);

                        SetFolder(await file.GetParentAsync());
                    }
                    catch (Exception ex)
                    {
                        openedFileContent.Text = ex.ToString();
                    }
                }

                await SetPickerResult(file is null ? null : new[] { file });
            };
            SaveFilePickerWithResult.Click += async delegate
            {
                var saveFileTypes = new[] { FilePickerFileTypes.Json, FilePickerFileTypes.Xml };
                var result = await GetStorageProvider().SaveFilePickerWithResultAsync(new FilePickerSaveOptions()
                {
                    Title = "Save file",
                    FileTypeChoices = saveFileTypes,
                    SuggestedFileType = GetSuggestedFileType(saveFileTypes),
                    SuggestedStartLocation = lastSelectedDirectory,
                    SuggestedFileName = "FileName",
                    ShowOverwritePrompt = true
                });

                try
                {
                    if (result.File is { } file)
                    {
                        // Sync disposal of StreamWriter is not supported on WASM
#if NET6_0_OR_GREATER
                        await using var stream = await file.OpenWriteAsync();
                        await using var writer = new System.IO.StreamWriter(stream);
#else
                        using var stream = await file.OpenWriteAsync();
                        using var writer = new System.IO.StreamWriter(stream);
#endif
                        if (result.SelectedFileType == FilePickerFileTypes.Xml)
                        {
                            await writer.WriteLineAsync("<sample>Test</sample>");
                        }
                        else
                        {
                            await writer.WriteLineAsync("""{ "sample": "Test" }""");
                        }

                        SetFolder(await result.File.GetParentAsync());
                    }
                }
                catch (Exception ex)
                {
                    openedFileContent.Text = ex.ToString();
                }

                await SetPickerResult(result.File is null ? null : new[] { result.File }, result.SelectedFileType);
            };
            OpenFolderPicker.Click += async delegate
            {
                var folders = await GetStorageProvider().OpenFolderPickerAsync(new FolderPickerOpenOptions()
                {
                    Title = "Folder file",
                    SuggestedStartLocation = lastSelectedDirectory,
                    SuggestedFileName = "FileName",
                    AllowMultiple = openMultiple.IsChecked == true
                });

                await SetPickerResult(folders);
            };
            OpenFileFromBookmark.Click += async delegate
            {
                var file = bookmarkContainer.Text is not null
                    ? await GetStorageProvider().OpenFileBookmarkAsync(bookmarkContainer.Text)
                    : null;

                await SetPickerResult(file is null ? null : new[] { file });
            };
            OpenFolderFromBookmark.Click += async delegate
            {
                var folder = bookmarkContainer.Text is not null
                    ? await GetStorageProvider().OpenFolderBookmarkAsync(bookmarkContainer.Text)
                    : null;

                await SetPickerResult(folder is null ? null : new[] { folder });
            };
        
            LaunchUri.Click += async delegate
            {
                var statusBlock = LaunchStatus;
                if (Uri.TryCreate(UriToLaunch.Text, UriKind.Absolute, out var uri))
                {
                    var result = await TopLevel.GetTopLevel(this)!.Launcher.LaunchUriAsync(uri);
                    statusBlock.Text = "LaunchUriAsync returned " + result;
                }
                else
                {
                    statusBlock.Text = "Can't parse the Uri";
                }
            };

            LaunchFile.Click += async delegate
            {
                var statusBlock = LaunchStatus;
                if (lastSelectedItem is not null)
                {
                    var result = await TopLevel.GetTopLevel(this)!.Launcher.LaunchFileAsync(lastSelectedItem);
                    statusBlock.Text = "LaunchFileAsync returned " + result;
                }
                else
                {
                    statusBlock.Text = "Please select any file or folder first";
                }
            };

            void SetFolder(IStorageFolder? folder)
            {
                ignoreTextChanged = true;
                lastSelectedDirectory = folder;
                lastSelectedItem = folder;
                currentFolderBox.Text = folder?.Path is { IsAbsoluteUri: true } abs ? abs.LocalPath : folder?.Path?.ToString();
                ignoreTextChanged = false;
            }
            async Task SetPickerResult(IReadOnlyCollection<IStorageItem>? items, FilePickerFileType? selectedType = null)
            {
                items ??= Array.Empty<IStorageItem>();
                bookmarkContainer.Text = items.FirstOrDefault(f => f.CanBookmark) is { } f ? await f.SaveBookmarkAsync() : "Can't bookmark";
                var mappedResults = new List<string>();

                string resultText = "";
                if (items.FirstOrDefault() is IStorageItem item)
                {
                    resultText += item is IStorageFile ? "File:" : "Folder:";
                    resultText += Environment.NewLine;

                    var props = await item.GetBasicPropertiesAsync();
                    resultText += @$"Size: {props.Size}
            DateCreated: {props.DateCreated}
            DateModified: {props.DateModified}
            CanBookmark: {item.CanBookmark}
            ";
                    if (item is IStorageFile file)
                    {
                        resultText += @$"
            Content:
            ";

                        try
                        {
                            resultText += await ReadTextFromFile(file, 500);
                        }
                        catch (Exception ex)
                        {
                            resultText += ex.ToString();
                        }
                    }

                    if (item is IStorageFolder storageFolder)
                    {
                        SetFolder(storageFolder);
                    }
                    else
                    {
                        var parent = await item.GetParentAsync();
                        SetFolder(parent);
                        if (parent is not null)
                        {
                            mappedResults.Add(FullPathOrName(parent));
                        }
                    }

                    foreach (var selectedItem in items)
                    {
                        mappedResults.Add("+> " + FullPathOrName(selectedItem));
                        if (selectedItem is IStorageFolder folder)
                        {
                            await foreach (var innerItem in folder.GetItemsAsync())
                            {
                                mappedResults.Add("++> " + FullPathOrName(innerItem));
                            }
                        }
                    }
                    lastSelectedItem = item;
                }

                if (selectedType is not null)
                {
                    resultText += Environment.NewLine + "Selected type: " + selectedType.Name;
                }

                openedFileContent.Text = resultText;
                results.ItemsSource = mappedResults;
                resultsVisible.IsVisible = mappedResults.Any();
            }
        }

        internal static async Task<string> ReadTextFromFile(IStorageFile file, int length)
        {
#if NET6_0_OR_GREATER
            await using var stream = await file.OpenReadAsync();
#else
            using var stream = await file.OpenReadAsync();
#endif
            using var reader = new System.IO.StreamReader(stream);

            // 4GB file test, shouldn't load more than 10000 chars into a memory.
            var buffer = ArrayPool<char>.Shared.Rent(length);
            try
            {
                var charsRead = await reader.ReadAsync(buffer, 0, length);
                return new string(buffer, 0, charsRead);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var openedFileContent = OpenedFileContent;
            try
            {
                var storageProvider = GetStorageProvider();
                openedFileContent.Text = $@"CanOpen: {storageProvider.CanOpen}
CanSave: {storageProvider.CanSave}
CanPickFolder: {storageProvider.CanPickFolder}";
            }
            catch (Exception ex)
            {
                openedFileContent.Text = "Storage provider is not available: " + ex.Message;
            }
        }

        private Window CreateSampleWindow()
        {
            Button button;
            Button dialogButton;

            var window = new Window
            {
                Height = 200,
                Width = 200,
                Content = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock { Text = "Hello world!" },
                        (button = new Button
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Content = "Click to close",
                            IsDefault = true
                        }),
                        (dialogButton = new Button
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Content = "Dialog",
                            IsDefault = false
                        })
                    }
                },
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            button.Click += (_, __) => window.Close();
            dialogButton.Click += (_, __) =>
            {
                var dialog = CreateSampleWindow();
                dialog.Height = 200;
                dialog.ShowDialog(window);
            };

            return window;
        }

        private IStorageProvider GetStorageProvider()
        {
            var forceManaged = ForceManaged.IsChecked ?? false;
            return forceManaged 
                ? new ManagedStorageProvider(GetWindow()) // NOTE: In your production App use 'AppBuilder.UseManagedSystemDialogs()'
                : GetTopLevel().StorageProvider;
        }

        private static string FullPathOrName(IStorageItem? item)
        {
            if (item is null) return "(null)";
            return item.Path is { IsAbsoluteUri: true } path ? path.ToString() : item.Name;
        }

        Window GetWindow() => TopLevel.GetTopLevel(this) as Window ?? throw new NullReferenceException("Invalid Owner");
        TopLevel GetTopLevel() => TopLevel.GetTopLevel(this) ?? throw new NullReferenceException("Invalid Owner");
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
