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
        private double _cellContentWidth;

        internal double CellContentWidth
        {
            get => _cellContentWidth;
            set
            {
                _cellContentWidth = value;

                if (_contentControl != null)
                    _contentControl.Width = value;
            }
        }


        private ContentControl _contentControl;

        public XDataGridCell()
        {
            this.TemplateApplied += TemplateAppliedCore;
        }

        private void TemplateAppliedCore(object sender, TemplateAppliedEventArgs e)
        {
            this._contentControl = e.NameScope.Find<ContentControl>("PART_ContentControl");

            if (Double.IsNaN(_contentControl.Width))
            {
                _contentControl.Width = _contentControl.Bounds.Width;
            }

        }
    }
}
