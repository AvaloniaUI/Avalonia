using System;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class DragAndDropPage : UserControl
    {
        TextBlock _DropState;
        private const string CustomFormat = "application/xxx-avalonia-controlcatalog-custom";
        public DragAndDropPage()
        {
            this.InitializeComponent();
            _DropState = this.Get<TextBlock>("DropState");

            int textCount = 0;
            SetupDnd("Text", d => d.Set(DataFormats.Text,
                $"Text was dragged {++textCount} times"), DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);

            SetupDnd("Custom", d => d.Set(CustomFormat, "Test123"), DragDropEffects.Move);
            SetupDnd("Files", d => d.Set(DataFormats.FileNames, new[] { Assembly.GetEntryAssembly()?.GetModules().FirstOrDefault()?.FullyQualifiedName }), DragDropEffects.Copy);
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
                    && !e.Data.Contains(DataFormats.FileNames)
                    && !e.Data.Contains(CustomFormat))
                    e.DragEffects = DragDropEffects.None;
            }

            void Drop(object? sender, DragEventArgs e)
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
                    _DropState.Text = e.Data.GetText();
                else if (e.Data.Contains(DataFormats.FileNames))
                    _DropState.Text = string.Join(Environment.NewLine, e.Data.GetFileNames() ?? Array.Empty<string>());
                else if (e.Data.Contains(CustomFormat))
                    _DropState.Text = "Custom: " + e.Data.Get(CustomFormat);
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
