using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using Avalonia.Platform.Storage;

namespace ControlCatalog.Pages
{
    public partial class DragAndDropPage : UserControl
    {
        private readonly DataFormat<string> _customFormat =
            DataFormat.CreateStringApplicationFormat("xxx-avalonia-controlcatalog-custom");

        public DragAndDropPage()
        {
            InitializeComponent();

            int textCount = 0;

            SetupDnd(
                "Text",
                d => d.Add(DataTransferItem.Create(DataFormat.Text, $"Text was dragged {++textCount} times")),
                DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);

            SetupDnd(
                "Custom",
                d => d.Add(DataTransferItem.Create(_customFormat, "Test123")),
                DragDropEffects.Copy | DragDropEffects.Move);

            SetupDnd(
                "Files",
                async d =>
                {
                    if (Assembly.GetEntryAssembly()?.GetModules().FirstOrDefault()?.FullyQualifiedName is { } name &&
                        TopLevel.GetTopLevel(this) is { } topLevel &&
                        await topLevel.StorageProvider.TryGetFileFromPathAsync(name) is { } storageFile)
                    {
                        d.Add(DataTransferItem.Create(DataFormat.File, storageFile));
                    }
                },
                DragDropEffects.Copy);

            SetupDnd(
                "Bitmap",
                d => d.Add(DataTransferItem.Create(DataFormat.Bitmap, new Bitmap(AssetLoader.Open(new Uri("avares://ControlCatalog/Assets/image1.jpg"))))),
                DragDropEffects.Copy);
        }

        private void SetupDnd(string suffix, Action<DataTransfer> factory, DragDropEffects effects) =>
            SetupDnd(
                suffix,
                o =>
                {
                    factory(o);
                    return Task.CompletedTask;
                },
                effects);

        private void SetupDnd(string suffix, Func<DataTransfer, Task> factory, DragDropEffects effects)
        {
            var dragMe = this.Get<Border>("DragMe" + suffix);
            var dragState = this.Get<TextBlock>("DragState" + suffix);

            async void DoDrag(object? sender, PointerPressedEventArgs e)
            {
                var dragData = new DataTransfer();
                await factory(dragData);

                var result = await DragDrop.DoDragDropAsync(e, dragData, effects);
                switch (result)
                {
                    case DragDropEffects.Move:
                        dragState.Text = "Data was moved";
                        break;
                    case DragDropEffects.Copy:
                        dragState.Text = "Data was copied";
                        break;
                    case DragDropEffects.Link:
                        dragState.Text = "Data was linked";
                        break;
                    case DragDropEffects.None:
                        dragState.Text = "The drag operation was canceled";
                        break;
                    default:
                        dragState.Text = "Unknown result";
                        break;
                }
            }

            void DragOver(object? sender, DragEventArgs e)
            {
                if (e.Source is Control c && c.Name == "MoveTarget")
                {
                    e.DragEffects = e.DragEffects & (DragDropEffects.Move);
                }
                else
                {
                    e.DragEffects = e.DragEffects & (DragDropEffects.Copy);
                }

                // Only allow if the dragged data contains text or filenames.
                if (!e.DataTransfer.Contains(DataFormat.Text)
                    && !e.DataTransfer.Contains(DataFormat.File)
                    && !e.DataTransfer.Contains(DataFormat.Bitmap)
                    && !e.DataTransfer.Contains(_customFormat))
                    e.DragEffects = DragDropEffects.None;
            }

            async void Drop(object? sender, DragEventArgs e)
            {
                if (e.Source is Control c && c.Name == "MoveTarget")
                {
                    e.DragEffects = e.DragEffects & (DragDropEffects.Move);
                }
                else
                {
                    e.DragEffects = e.DragEffects & (DragDropEffects.Copy);
                }

                if (e.DataTransfer.Contains(DataFormat.Text))
                {
                    DropState.Content = e.DataTransfer.TryGetText();
                }
                else if (e.DataTransfer.Contains(DataFormat.File))
                {
                    var files = e.DataTransfer.TryGetFiles() ?? [];
                    var contentStr = "";

                    foreach (var item in files)
                    {
                        if (item is IStorageFile file)
                        {
                            var content = await DialogsPage.ReadTextFromFile(file, 500);
                            contentStr += $"File {item.Name}:{Environment.NewLine}{content}{Environment.NewLine}{Environment.NewLine}";
                        }
                        else if (item is IStorageFolder folder)
                        {
                            var childrenCount = 0;
                            await foreach (var _ in folder.GetItemsAsync())
                            {
                                childrenCount++;
                            }
                            contentStr += $"Folder {item.Name}: items {childrenCount}{Environment.NewLine}{Environment.NewLine}";
                        }
                    }
                    DropState.Content = contentStr;
                }
                else if (e.DataTransfer.Contains(DataFormat.Bitmap))
                {
                    var bitmap = e.DataTransfer.TryGetValue(DataFormat.Bitmap);
                    DropState.Content = new Image
                    {
                        Source = bitmap, Width = 400, Height = 300, Stretch = Stretch.Uniform
                    };
                }
                else if (e.DataTransfer.Contains(_customFormat))
                {
                    DropState.Content = "Custom: " + e.DataTransfer.TryGetValue(_customFormat);
                }
            }

            dragMe.PointerPressed += DoDrag;

            AddHandler(DragDrop.DropEvent, Drop);
            AddHandler(DragDrop.DragOverEvent, DragOver);
        }
    }
}
