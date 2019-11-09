using System;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using ReactiveUI;

namespace RenderDemo.Pages
{
    public class DataRepeaterCell : ContentControl
    {
        internal string _targetProperty;
        internal DataRepeaterCellContent _cellContent;

        public DataRepeaterCell()
        {
            TemplateApplied += TemplateAppliedCore;
        }

        private void TemplateAppliedCore(object sender, TemplateAppliedEventArgs e)
        {
            _cellContent = e.NameScope.Find<DataRepeaterCellContent>("PART_CellContent");

            _cellContent.Classes.Add(_targetProperty);

            var newBind = new Binding(_targetProperty, BindingMode.TwoWay);

            _cellContent.Bind(DataRepeaterCellContent.CellValueProperty, newBind);
            _cellContent.Classes.Add(_targetProperty);
        }
    }
}
