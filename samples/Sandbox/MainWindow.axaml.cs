using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using System;



namespace Sandbox
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnTextPointerPressed(object sender, PointerPressedEventArgs e)
        {
            var text = "Example Data";
            var dataObject = new DataObject();
            dataObject.Set(DataFormats.Text, text);

            var startPos = e.GetPosition(this);
            Console.WriteLine($"Pointer Pressed at: {startPos}");

            DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Copy);
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            var position = e.GetPosition((Control)sender);
            Console.WriteLine($"Dragging over position: {position}");

            e.DragEffects = DragDropEffects.Copy | DragDropEffects.Move;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            var dropTarget = (Control)sender;
            var dropPosition = e.GetPosition(dropTarget);

            Console.WriteLine($"Dropped at: {dropPosition}");

            ShowContextMenu(dropTarget, dropPosition);
            e.Handled = true;
        }

        private void ShowContextMenu(Control targetControl, Point position)
        {
            Console.WriteLine($"Menu showed at: {position}");
            var menuFlyout = new MenuFlyout();

            menuFlyout.Items.Add(new MenuItem { Header = "Option 1" });
            menuFlyout.Items.Add(new MenuItem { Header = "Option 2" });
            menuFlyout.Items.Add(new MenuItem { Header = "Option 3" });

            menuFlyout.Placement = PlacementMode.Pointer;

            menuFlyout.ShowAt(targetControl);
        }
    }
}
