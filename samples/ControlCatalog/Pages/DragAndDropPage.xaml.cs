﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace ControlCatalog.Pages
{
    public class DragAndDropPage : UserControl
    {
        private readonly TextBlock _dropState;
        private const string CustomFormat = "application/xxx-avalonia-controlcatalog-custom";
        public DragAndDropPage()
        {
            this.InitializeComponent();
            _dropState = this.Get<TextBlock>("DropState");

            int textCount = 0;
            SetupDnd("Text", d => d.Set(DataFormats.Text,
                $"Text was dragged {++textCount} times"), DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);

            SetupDnd("Custom", d => d.Set(CustomFormat, "Test123"), DragDropEffects.Move);
            SetupDnd("Files", d => d.Set(DataFormats.Files, new[] { Assembly.GetEntryAssembly()?.GetModules().FirstOrDefault()?.FullyQualifiedName }), DragDropEffects.Copy);
        }

        void SetupDnd(string suffix, Action<DataObject> factory, DragDropEffects effects)
        {
            var dragMe = this.Get<Border>("DragMe" + suffix);
            var dragState = this.Get<TextBlock>("DragState" + suffix);

            async void DoDrag(object? sender, Avalonia.Input.PointerPressedEventArgs e)
            {
                var dragData = new DataObject();
                factory(dragData);

                var result = await DragDrop.DoDragDrop(e, dragData, effects);
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
                if (!e.Data.Contains(DataFormats.Text)
                    && !e.Data.Contains(DataFormats.Files)
                    && !e.Data.Contains(CustomFormat))
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

                if (e.Data.Contains(DataFormats.Text))
                {
                    _dropState.Text = e.Data.GetText();
                }
                else if (e.Data.Contains(DataFormats.Files))
                {
                    var files = e.Data.GetFiles() ?? Array.Empty<IStorageItem>();
                    var contentStr = "";

                    foreach (var item in files)
                    {
                        if (item is IStorageFile file)
                        {
                            var content = await DialogsPage.ReadTextFromFile(file, 1000);
                            contentStr += $"File {item.Name}:{Environment.NewLine}{content}{Environment.NewLine}{Environment.NewLine}";
                        }
                        else if (item is IStorageFolder folder)
                        {
                            var items = await folder.GetItemsAsync();
                            contentStr += $"Folder {item.Name}: items {items.Count}{Environment.NewLine}{Environment.NewLine}";
                        }
                    }

                    _dropState.Text = contentStr;
                }
#pragma warning disable CS0618 // Type or member is obsolete
                else if (e.Data.Contains(DataFormats.FileNames))
                {
                    var files = e.Data.GetFileNames();
                    _dropState.Text = string.Join(Environment.NewLine, files ?? Array.Empty<string>());
                }
#pragma warning restore CS0618 // Type or member is obsolete
                else if (e.Data.Contains(CustomFormat))
                {
                    _dropState.Text = "Custom: " + e.Data.Get(CustomFormat);
                }
            }

            dragMe.PointerPressed += DoDrag;

            AddHandler(DragDrop.DropEvent, Drop);
            AddHandler(DragDrop.DragOverEvent, DragOver);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
