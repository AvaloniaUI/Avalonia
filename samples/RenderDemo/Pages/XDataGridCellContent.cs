using Avalonia;
using Avalonia.Controls;

namespace RenderDemo.Pages
{
    public class XDataGridCellContent : ContentControl
    {
        public static readonly DirectProperty<XDataGridCellContent, object> CellValueProperty =
            AvaloniaProperty.RegisterDirect<XDataGridCellContent, object>(
                nameof(CellValue),
                o => o.CellValue,
                (o, v) => o.CellValue = v);

        private object _cellValue;

        public object CellValue
        {
            get
            {
                return _cellValue;
            }
            set
            {
                SetAndRaise(CellValueProperty, ref _cellValue, value);
            }
        }
    }
}
