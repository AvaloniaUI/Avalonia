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
    public class XDataGridCell : ContentControl
    {
        internal string TargetProperty;
        internal XDataGridCellContent _cellContent;

        public XDataGridCell()
        {
            this.TemplateApplied += TemplateAppliedCore;
        }

        private void TemplateAppliedCore(object sender, TemplateAppliedEventArgs e)
        {
            this._cellContent = e.NameScope.Find<XDataGridCellContent>("PART_CellContent");

            _cellContent.Classes.Add(TargetProperty);

            var newBind = new Binding(TargetProperty, BindingMode.TwoWay);

            _cellContent.Bind(XDataGridCellContent.CellValueProperty, newBind);
            _cellContent.Classes.Add(TargetProperty);
        }
    }
}
