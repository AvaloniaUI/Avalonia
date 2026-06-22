using Avalonia.Controls;
using Avalonia.Input;

namespace ControlCatalog
{
    public partial class DecoratedWindow : Window
    {
        public DecoratedWindow()
        {
            InitializeComponent();
            TitleBar.PointerPressed += (i, e) =>
            {
                BeginMoveDrag(e);
            };
            SetupSide(Left, StandardCursorType.LeftSide, WindowEdge.West);
            SetupSide(Right, StandardCursorType.RightSide, WindowEdge.East);
            SetupSide(Top, StandardCursorType.TopSide, WindowEdge.North);
            SetupSide(Bottom, StandardCursorType.BottomSide, WindowEdge.South);
            SetupSide(TopLeft, StandardCursorType.TopLeftCorner, WindowEdge.NorthWest);
            SetupSide(TopRight, StandardCursorType.TopRightCorner, WindowEdge.NorthEast);
            SetupSide(BottomLeft, StandardCursorType.BottomLeftCorner, WindowEdge.SouthWest);
            SetupSide(BottomRight, StandardCursorType.BottomRightCorner, WindowEdge.SouthEast);
            MinimizeButton.Click += delegate
            { this.WindowState = WindowState.Minimized; };
            MaximizeButton.Click += delegate
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            };
            CloseButton.Click += delegate
            {
                Close();
            };
        }

        private void SetupSide(Control ctl, StandardCursorType cursor, WindowEdge edge)
        {
            ctl.Cursor = new Cursor(cursor);
            ctl.PointerPressed += (i, e) =>
            {
                if (WindowState == WindowState.Normal)
                    BeginResizeDrag(edge, e);
            };
        }
    }
}
