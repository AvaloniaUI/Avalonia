using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ControlCatalog.Pages
{
    public class DragAndDropPage : UserControl
    {
        TextBlock _DropState;
        private const string CustomFormat = "application/xxx-avalonia-controlcatalog-custom";
        public DragAndDropPage()
        {
            this.InitializeComponent();
            _DropState = this.Find<TextBlock>("DropState");

            int textCount = 0;
            SetupDnd("Text", d => d.Set(DataFormats.Text,
                $"Text was dragged {++textCount} times"));

            SetupDnd("Custom", d => d.Set(CustomFormat, "Test123"));
        }

        void SetupDnd(string suffix, Action<DataObject> factory, DragDropEffects effects = DragDropEffects.Copy)
        {
            var dragMe = this.Find<Border>("DragMe" + suffix);
            var dragState = this.Find<TextBlock>("DragState"+suffix);

            async void DoDrag(object sender, Avalonia.Input.PointerPressedEventArgs e)
            {
                var dragData = new DataObject();
                factory(dragData);

                var result = await DragDrop.DoDragDrop(e, dragData, DragDropEffects.Copy);
                switch (result)
                {
                    case DragDropEffects.Copy:
                        dragState.Text = "Data was copied";
                        break;
                    case DragDropEffects.Link:
                        dragState.Text = "Data was linked";
                        break;
                    case DragDropEffects.None:
                        dragState.Text = "The drag operation was canceled";
                        break;
                }
            }

            void DragOver(object sender, DragEventArgs e)
            {
                // Only allow Copy or Link as Drop Operations.
                e.DragEffects = e.DragEffects & (DragDropEffects.Copy | DragDropEffects.Link);

                // Only allow if the dragged data contains text or filenames.
                if (!e.Data.Contains(DataFormats.Text)
                    && !e.Data.Contains(DataFormats.FileNames)
                    && !e.Data.Contains(CustomFormat))
                    e.DragEffects = DragDropEffects.None;
            }

            void Drop(object sender, DragEventArgs e)
            {
                if (e.Data.Contains(DataFormats.Text))
                    _DropState.Text = e.Data.GetText();
                else if (e.Data.Contains(DataFormats.FileNames))
                    _DropState.Text = string.Join(Environment.NewLine, e.Data.GetFileNames());
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
