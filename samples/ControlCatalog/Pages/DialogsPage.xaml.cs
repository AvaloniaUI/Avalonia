using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public class DialogsPage : UserControl
    {
        public DialogsPage()
        {
            this.InitializeComponent();

////            var results = this.Get<ItemsPresenter>("PickerLastResults");
////            var resultsVisible = this.Get<TextBlock>("PickerLastResultsVisible");
////            var bookmarkContainer = this.Get<TextBox>("BookmarkContainer");
////            var openedFileContent = this.Get<TextBox>("OpenedFileContent");
////            var openMultiple = this.Get<CheckBox>("OpenMultiple");

////            IStorageFolder? lastSelectedDirectory = null;

////            List<FileDialogFilter> GetFilters()
////            {
////                if (this.Get<CheckBox>("UseFilters").IsChecked != true)
////                    return new List<FileDialogFilter>();
////                return  new List<FileDialogFilter>
////                {
////                    new FileDialogFilter
////                    {
////                        Name = "Text files (.txt)", Extensions = new List<string> {"txt"}
////                    },
////                    new FileDialogFilter
////                    {
////                        Name = "All files",
////                        Extensions = new List<string> {"*"}
////                    }
////                };
////            }

////            List<FilePickerFileType>? GetFileTypes()
////            {
////                if (this.Get<CheckBox>("UseFilters").IsChecked != true)
////                    return null;
////                return new List<FilePickerFileType>
////                {
////                    FilePickerFileTypes.All,
////                    FilePickerFileTypes.TextPlain
////                };
////            }

////            this.Get<Button>("OpenFile").Click += async delegate
////            {
////                // Almost guaranteed to exist
////                var uri = Assembly.GetEntryAssembly()?.GetModules().FirstOrDefault()?.FullyQualifiedName;
////                var initialFileName = uri == null ? null : System.IO.Path.GetFileName(uri);
////                var initialDirectory = uri == null ? null : System.IO.Path.GetDirectoryName(uri);

////                var result = await new OpenFileDialog()
////                {
////                    Title = "Open file",
////                    Filters = GetFilters(),
////                    Directory = initialDirectory,
////                    InitialFileName = initialFileName
////                }.ShowAsync(GetWindow());
////                results.Items = result;
////                resultsVisible.IsVisible = result?.Any() == true;
////            };
////            this.Get<Button>("OpenMultipleFiles").Click += async delegate
////            {
////                var result = await new OpenFileDialog()
////                {
////                    Title = "Open multiple files",
////                    Filters = GetFilters(),
////                    Directory = lastSelectedDirectory?.TryGetUri(out var path) == true ? path.LocalPath : null,
////                    AllowMultiple = true
////                }.ShowAsync(GetWindow());
////                results.Items = result;
////                resultsVisible.IsVisible = result?.Any() == true;
////            };
////            this.Get<Button>("SaveFile").Click += async delegate
////            {
////                var filters = GetFilters();
////                var result = await new SaveFileDialog()
////                {
////                    Title = "Save file",
////                    Filters = filters,
////                    Directory = lastSelectedDirectory?.TryGetUri(out var path) == true ? path.LocalPath : null,
////                    DefaultExtension = filters?.Any() == true ? "txt" : null,
////                    InitialFileName = "test.txt"
////                }.ShowAsync(GetWindow());
////                results.Items = new[] { result };
////                resultsVisible.IsVisible = result != null;
////            };
////            this.Get<Button>("SelectFolder").Click += async delegate
////            {
////                var result = await new OpenFolderDialog()
////                {
////                    Title = "Select folder",
////                    Directory = lastSelectedDirectory?.TryGetUri(out var path) == true ? path.LocalPath : null
////                }.ShowAsync(GetWindow());
////                if (string.IsNullOrEmpty(result))
////                {
////                    resultsVisible.IsVisible = false;
////                }
////                else
////                {
////                    lastSelectedDirectory = new BclStorageFolder(new System.IO.DirectoryInfo(result));
////                    results.Items = new[] { result };
////                    resultsVisible.IsVisible = true;
////                }
////            };
////            this.Get<Button>("OpenBoth").Click += async delegate
////            {
////                var result = await new OpenFileDialog()
////                {
////                    Title = "Select both",
////                    Directory = lastSelectedDirectory?.TryGetUri(out var path) == true ? path.LocalPath : null,
////                    AllowMultiple = true
////                }.ShowManagedAsync(GetWindow(), new ManagedFileDialogOptions
////                {
////                    AllowDirectorySelection = true
////                });
////                results.Items = result;
////                resultsVisible.IsVisible = result?.Any() == true;
////            };
////            this.Get<Button>("DecoratedWindow").Click += delegate
////            {
////                new DecoratedWindow().Show();
////            };
////            this.Get<Button>("DecoratedWindowDialog").Click += delegate
////            {
////                _ = new DecoratedWindow().ShowDialog(GetWindow());
////            };
////            this.Get<Button>("Dialog").Click += delegate
////            {
////                var window = CreateSampleWindow();
////                window.Height = 200;
////                _ = window.ShowDialog(GetWindow());
////            };
////            this.Get<Button>("DialogNoTaskbar").Click += delegate
////            {
////                var window = CreateSampleWindow();
////                window.Height = 200;
////                window.ShowInTaskbar = false;
////                _ = window.ShowDialog(GetWindow());
////            };
////            this.Get<Button>("OwnedWindow").Click += delegate
////            {
////                var window = CreateSampleWindow();

////                window.Show(GetWindow());
////            };

////            this.Get<Button>("OwnedWindowNoTaskbar").Click += delegate
////            {
////                var window = CreateSampleWindow();

////                window.ShowInTaskbar = false;

////                window.Show(GetWindow());
////            };

////            this.Get<Button>("OpenFilePicker").Click += async delegate
////            {
////                var result = await GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions()
////                {
////                    Title = "Open file",
////                    FileTypeFilter = GetFileTypes(),
////                    SuggestedStartLocation = lastSelectedDirectory,
////                    AllowMultiple = openMultiple.IsChecked == true
////                });

////                await SetPickerResult(result);
////            };
////            this.Get<Button>("SaveFilePicker").Click += async delegate
////            {
////                var fileTypes = GetFileTypes();
////                var file = await GetStorageProvider().SaveFilePickerAsync(new FilePickerSaveOptions()
////                {
////                    Title = "Save file",
////                    FileTypeChoices = fileTypes,
////                    SuggestedStartLocation = lastSelectedDirectory,
////                    SuggestedFileName = "FileName",
////                    DefaultExtension = fileTypes?.Any() == true ? "txt" : null,
////                    ShowOverwritePrompt = false
////                });

////                if (file is not null && file.CanOpenWrite)
////                {
////                    // Sync disposal of StreamWriter is not supported on WASM
////#if NET6_0_OR_GREATER
////                    await using var stream = await file.OpenWriteAsync();
////                    await using var reader = new System.IO.StreamWriter(stream);
////#else
////                    using var stream = await file.OpenWriteAsync();
////                    using var reader = new System.IO.StreamWriter(stream);
////#endif
////                    await reader.WriteLineAsync(openedFileContent.Text);

////                    lastSelectedDirectory = await file.GetParentAsync();
////                }

////                await SetPickerResult(file is null ? null : new [] {file});
////            };
////            this.Get<Button>("OpenFolderPicker").Click += async delegate
////            {
////                var folders = await GetStorageProvider().OpenFolderPickerAsync(new FolderPickerOpenOptions()
////                {
////                    Title = "Folder file",
////                    SuggestedStartLocation = lastSelectedDirectory,
////                    AllowMultiple = openMultiple.IsChecked == true
////                });

////                await SetPickerResult(folders);

////                lastSelectedDirectory = folders.FirstOrDefault();
////            };
////            this.Get<Button>("OpenFileFromBookmark").Click += async delegate
////            {
////                var file = bookmarkContainer.Text is not null
////                    ? await GetStorageProvider().OpenFileBookmarkAsync(bookmarkContainer.Text)
////                    : null;

////                await SetPickerResult(file is null ? null : new[] { file });
////            };
////            this.Get<Button>("OpenFolderFromBookmark").Click += async delegate
////            {
////                var folder = bookmarkContainer.Text is not null
////                    ? await GetStorageProvider().OpenFolderBookmarkAsync(bookmarkContainer.Text)
////                    : null;

////                await SetPickerResult(folder is null ? null : new[] { folder });
                
////                lastSelectedDirectory = folder;
////            };

////            async Task SetPickerResult(IReadOnlyCollection<IStorageItem>? items)
////            {
////                items ??= Array.Empty<IStorageItem>();
////                bookmarkContainer.Text = items.FirstOrDefault(f => f.CanBookmark) is { } f ? await f.SaveBookmarkAsync() : "Can't bookmark";
////                var mappedResults = new List<string>();

////                if (items.FirstOrDefault() is IStorageItem item)
////                {
////                    var resultText = item is IStorageFile ? "File:" : "Folder:";
////                    resultText += Environment.NewLine;

////                    var props = await item.GetBasicPropertiesAsync();
////                    resultText += @$"Size: {props.Size}
////DateCreated: {props.DateCreated}
////DateModified: {props.DateModified}
////CanBookmark: {item.CanBookmark}
////";
////                    if (item is IStorageFile file)
////                    {
////                        resultText += @$"
////CanOpenRead: {file.CanOpenRead}
////CanOpenWrite: {file.CanOpenWrite}
////Content:
////";
////                        if (file.CanOpenRead)
////                        {
////#if NET6_0_OR_GREATER
////                            await using var stream = await file.OpenReadAsync();
////#else
////                            using var stream = await file.OpenReadAsync();
////#endif
////                            using var reader = new System.IO.StreamReader(stream);

////                            // 4GB file test, shouldn't load more than 10000 chars into a memory.
////                            const int length = 10000;
////                            var buffer = ArrayPool<char>.Shared.Rent(length);
////                            try
////                            {
////                                var charsRead = await reader.ReadAsync(buffer, 0, length);
////                                resultText += new string(buffer, 0, charsRead);
////                            }
////                            finally
////                            {
////                                ArrayPool<char>.Shared.Return(buffer);
////                            }
////                        }
////                    }

////                    openedFileContent.Text = resultText;

////                    lastSelectedDirectory = await item.GetParentAsync();
////                    if (lastSelectedDirectory is not null)
////                    {
////                        mappedResults.Add(FullPathOrName(lastSelectedDirectory));
////                    }

////                    foreach (var selectedItem in items)
////                    {
////                        mappedResults.Add("+> " + FullPathOrName(selectedItem));
////                        if (selectedItem is IStorageFolder folder)
////                        {
////                            foreach (var innerItems in await folder.GetItemsAsync())
////                            {
////                                mappedResults.Add("++> " + FullPathOrName(innerItems));
////                            }
////                        }
////                    }
////                }

////                results.Items = mappedResults;
////                resultsVisible.IsVisible = mappedResults.Any();
////            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            var openedFileContent = this.Get<TextBox>("OpenedFileContent");
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
            var forceManaged = this.Get<CheckBox>("ForceManaged").IsChecked ?? false;
            return forceManaged
                ? new ManagedStorageProvider<Window>(GetWindow(), null)
                : GetTopLevel().StorageProvider;
        }

        private static string FullPathOrName(IStorageItem? item)
        {
            if (item is null) return "(null)";
            return item.TryGetUri(out var uri) ? uri.ToString() : item.Name;
        }

        Window GetWindow() => this.VisualRoot as Window ?? throw new NullReferenceException("Invalid Owner");
        TopLevel GetTopLevel() => this.VisualRoot as TopLevel ?? throw new NullReferenceException("Invalid Owner");

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
