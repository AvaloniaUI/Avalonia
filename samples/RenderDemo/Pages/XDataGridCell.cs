using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ReactiveUI;

namespace RenderDemo.Pages
{
    public class XDataGridCell : ContentControl
    { 
        internal ContentControl _contentControl;

        public XDataGridCell()
        {
            this.TemplateApplied += TemplateAppliedCore; 
        } 

        private void TemplateAppliedCore(object sender, TemplateAppliedEventArgs e)
        {
            this._contentControl = e.NameScope.Find<ContentControl>("PART_ContentControl");
        }
    }
}
