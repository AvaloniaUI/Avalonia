using Avalonia;
using Avalonia.Controls;

namespace RenderDemo.Pages
{
    public class DataRepeaterCellContent : ContentControl
    {
        public static readonly DirectProperty<DataRepeaterCellContent, object> CellValueProperty =
            AvaloniaProperty.RegisterDirect<DataRepeaterCellContent, object>(
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
