using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using Avalonia.Input;

namespace ControlCatalog
{
    public class DecoratedWindow : Window
    {
        public DecoratedWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
        }

        void SetupSide(string name, StandardCursorType cursor, WindowEdge edge)
        {
            var ctl = this.FindControl<Control>(name);
            ctl.Cursor = new Cursor(cursor);
            ctl.PointerPressed += delegate
            {
                PlatformImpl?.BeginResizeDrag(edge);
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<Control>("TitleBar").PointerPressed += delegate
            {
                PlatformImpl?.BeginMoveDrag();
            };
            SetupSide("Left", StandardCursorType.LeftSide, WindowEdge.West);
            SetupSide("Right", StandardCursorType.RightSide, WindowEdge.East);
            SetupSide("Top", StandardCursorType.TopSide, WindowEdge.North);
            SetupSide("Bottom", StandardCursorType.BottomSize, WindowEdge.South);
            SetupSide("TopLeft", StandardCursorType.TopLeftCorner, WindowEdge.NorthWest);
            SetupSide("TopRight", StandardCursorType.TopRightCorner, WindowEdge.NorthEast);
            SetupSide("BottomLeft", StandardCursorType.BottomLeftCorner, WindowEdge.SouthWest);
            SetupSide("BottomRight", StandardCursorType.BottomRightCorner, WindowEdge.SouthEast);
            this.FindControl<Button>("MinimizeButton").Click += delegate { this.WindowState = WindowState.Minimized; };
            this.FindControl<Button>("MaximizeButton").Click += delegate
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            };
            this.FindControl<Button>("CloseButton").Click += delegate
            {
                Close();
            };
        }
    }
}
