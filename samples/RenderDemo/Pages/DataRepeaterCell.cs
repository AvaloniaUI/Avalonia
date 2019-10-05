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
        internal string TargetProperty;
        internal DataRepeaterCellContent _cellContent;

        public DataRepeaterCell()
        {
            this.TemplateApplied += TemplateAppliedCore;
        }

        private void TemplateAppliedCore(object sender, TemplateAppliedEventArgs e)
        {
            this._cellContent = e.NameScope.Find<DataRepeaterCellContent>("PART_CellContent");

            _cellContent.Classes.Add(TargetProperty);

            var newBind = new Binding(TargetProperty, BindingMode.TwoWay);

            _cellContent.Bind(DataRepeaterCellContent.CellValueProperty, newBind);
            _cellContent.Classes.Add(TargetProperty);
        }
    }
}
