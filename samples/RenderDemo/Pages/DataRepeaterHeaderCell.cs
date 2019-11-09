using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using ReactiveUI;

namespace RenderDemo.Pages
{
    public class DataRepeaterHeaderCell : ContentControl
    {
        private Thumb _rightThumbResizer;
        internal ContentControl _contentControl;

        public DataRepeaterHeaderCell()
        {
            TemplateApplied += TemplateAppliedCore;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var desc = (Content as DataRepeaterHeaderDescriptor);

            if (_contentControl != null || desc != null)
            {
                var content = (Content as DataRepeaterHeaderDescriptor);
                content.HeaderWidth = _contentControl.Bounds.Width;
            }

            return base.MeasureOverride(availableSize);
        }

        private void TemplateAppliedCore(object sender, TemplateAppliedEventArgs e)
        {

            _contentControl = e.NameScope.Find<ContentControl>("PART_ContentControl");

            _rightThumbResizer = e.NameScope.Find<Thumb>("PART_RightThumbResizer");

            var content = (Content as DataRepeaterHeaderDescriptor);
            content.HeaderWidth = _contentControl.Bounds.Width;


            if (_rightThumbResizer == null)
                return;
            _rightThumbResizer.DragDelta += ResizerDragDelta;
            _rightThumbResizer.DragStarted += ResizerDragStarted;
            _rightThumbResizer.Cursor = new Cursor(StandardCursorType.SizeWestEast);

            this.PointerPressed += HeaderCell_PointerPressed;
        }

        private void HeaderCell_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.MouseButton == MouseButton.Left)
            {
                e.Handled = true;

                HeaderSort();
            }
        }

        private void HeaderSort()
        {
            var desc = (Content as DataRepeaterHeaderDescriptor);
            var parent = (Parent as DataRepeaterHeader);

            if (_contentControl != null || desc != null || parent != null)
            { 
                var dx = parent.HeaderDescriptors;
                dx.SortDescriptor(desc);
            }
        }

        private void ResizerDragStarted(object sender, VectorEventArgs e)
        {
            if (!double.IsNaN(_contentControl.Width))
                return;

            _contentControl.Width = _contentControl.Bounds.Width;
        }

        private void ResizerDragDelta(object sender, VectorEventArgs e)
        {
            var newW = _contentControl.Width + e.Vector.X;

            if (newW <= 0)
                return;

            _contentControl.Width = newW;
        }
    }
}
