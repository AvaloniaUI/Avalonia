// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class DataGridTemplateColumn : DataGridColumn
    {
        IDataTemplate _cellTemplate;

        public static readonly DirectProperty<DataGridTemplateColumn, IDataTemplate> CellTemplateProperty =
            AvaloniaProperty.RegisterDirect<DataGridTemplateColumn, IDataTemplate>(
                nameof(CellTemplate),
                o => o.CellTemplate,
                (o, v) => o.CellTemplate = v);

        public IDataTemplate CellTemplate
        {
            get { return _cellTemplate; }
            set { SetAndRaise(CellTemplateProperty, ref _cellTemplate, value); }
        }

        private void OnCellTemplateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldValue = (IDataTemplate)e.OldValue;
            var value = (IDataTemplate)e.NewValue;
        }

        public DataGridTemplateColumn()
        {
            IsReadOnly = true;
        }

        protected override IControl GenerateElement(DataGridCell cell, object dataItem)
        {
            if(CellTemplate != null)
            {
                return CellTemplate.Build(dataItem);
            }
            if (Design.IsDesignMode)
            {
                return null;
            }
            else
            {
                throw DataGridError.DataGridTemplateColumn.MissingTemplateForType(typeof(DataGridTemplateColumn));
            }
        }

        protected override IControl GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding)
        {
            binding = null;
            return GenerateElement(cell, dataItem);
        }

        protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs)
        {
            return null;
        }

        protected internal override void RefreshCellContent(IControl element, string propertyName)
        {
            if(propertyName == nameof(CellTemplate) && element.Parent is DataGridCell cell)
            {
                cell.Content = GenerateElement(cell, cell.DataContext);
            }

            base.RefreshCellContent(element, propertyName);
        }
    }
}
