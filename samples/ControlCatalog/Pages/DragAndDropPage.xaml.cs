using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace ControlCatalog.Pages
{
    public class DragAndDropPage : UserControl
    {
        private TextBlock _DropState;
        private TextBlock _DragState;
        private Border _DragMe;
        private int DragCount = 0;

        public DragAndDropPage()
        {
            this.InitializeComponent();

            _DragMe.PointerPressed += DoDrag;

            AddHandler(DragDrop.DropEvent, Drop);
            AddHandler(DragDrop.DragOverEvent, DragOver);
        }

        private async void DoDrag(object sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            DataObject dragData = new DataObject();
            dragData.Set(DataFormats.Text, $"You have dragged text {++DragCount} times");

            var result = await DragDrop.DoDragDrop(dragData, DragDropEffects.Copy);
            switch(result)
            {
                case DragDropEffects.Copy:
                    _DragState.Text = "The text was copied"; break;
                case DragDropEffects.Link:
                    _DragState.Text = "The text was linked"; break;
                case DragDropEffects.None:
                    _DragState.Text = "The drag operation was canceled"; break;
            }
        }

        private void DragOver(object sender, DragEventArgs e)
        {
            // Only allow Copy or Link as Drop Operations.
            e.DragEffects = e.DragEffects & (DragDropEffects.Copy | DragDropEffects.Link);

            // Only allow if the dragged data contains text or filenames.
            if (!e.Data.Contains(DataFormats.Text) && !e.Data.Contains(DataFormats.FileNames))
                e.DragEffects = DragDropEffects.None; 
        }

        private void Drop(object sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Text))
                _DropState.Text = e.Data.GetText();
            else if (e.Data.Contains(DataFormats.FileNames))
                _DropState.Text = string.Join(Environment.NewLine, e.Data.GetFileNames());
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _DropState = this.Find<TextBlock>("DropState");
            _DragState = this.Find<TextBlock>("DragState");
            _DragMe = this.Find<Border>("DragMe");
        }
    }
}
