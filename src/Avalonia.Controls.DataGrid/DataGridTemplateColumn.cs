// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class DataGridTemplateColumn : DataGridColumn
    {
        private IDataTemplate _cellTemplate;

        public static readonly DirectProperty<DataGridTemplateColumn, IDataTemplate> CellTemplateProperty =
            AvaloniaProperty.RegisterDirect<DataGridTemplateColumn, IDataTemplate>(
                nameof(CellTemplate),
                o => o.CellTemplate,
                (o, v) => o.CellTemplate = v);

        [Content]
        public IDataTemplate CellTemplate
        {
            get { return _cellTemplate; }
            set { SetAndRaise(CellTemplateProperty, ref _cellTemplate, value); }
        }

        private IDataTemplate _cellEditingCellTemplate;

        /// <summary>
        /// Defines the <see cref="CellEditingTemplate"/> property.
        /// </summary>
        public static readonly DirectProperty<DataGridTemplateColumn, IDataTemplate> CellEditingTemplateProperty =
                AvaloniaProperty.RegisterDirect<DataGridTemplateColumn, IDataTemplate>(
                    nameof(CellEditingTemplate),
                    o => o.CellEditingTemplate,
                    (o, v) => o.CellEditingTemplate = v);

        /// <summary>
        /// Gets or sets the <see cref="IDataTemplate"/> which is used for the editing mode of the current <see cref="DataGridCell"/>
        /// </summary>
        /// <value>
        /// An <see cref="IDataTemplate"/> for the editing mode of the current <see cref="DataGridCell"/>
        /// </value>
        /// <remarks>
        /// If this property is <see langword="null"/> the column is read-only.
        /// </remarks>
        public IDataTemplate CellEditingTemplate
        {
            get => _cellEditingCellTemplate;
            set => SetAndRaise(CellEditingTemplateProperty, ref _cellEditingCellTemplate, value);
        }
        
        private static void OnCellTemplateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldValue = (IDataTemplate)e.OldValue;
            var value = (IDataTemplate)e.NewValue;
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
            if(CellEditingTemplate != null)
            {
                return CellEditingTemplate.Build(dataItem);
            }
            else if (CellTemplate != null)
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

        protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs)
        {
            return null;
        }

        protected internal override void RefreshCellContent(IControl element, string propertyName)
        {
            var cell = element.Parent as DataGridCell;
            if(propertyName == nameof(CellTemplate) && cell is not null)
            {
                cell.Content = GenerateElement(cell, cell.DataContext);
            }

            base.RefreshCellContent(element, propertyName);
        }
        
        public override bool IsReadOnly
        {
            get
            {
                if (CellEditingTemplate is null)
                {
                    return true;
                }

                return base.IsReadOnly;
            }
            set
            {
                base.IsReadOnly = value;
            }
        }
    }
}
