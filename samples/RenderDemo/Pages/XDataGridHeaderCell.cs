using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using ReactiveUI;

namespace RenderDemo.Pages
{
    public class XDataGridHeaderCell : ContentControl
    {
        private Thumb _rightThumbResizer;
        private ContentControl _contentControl;

        public XDataGridHeaderCell()
        {
            this.TemplateApplied += TemplateAppliedCore;
            this.Cursor = new Cursor(StandardCursorType.SizeWestEast);

        }

        private void TemplateAppliedCore(object sender, TemplateAppliedEventArgs e)
        {
            this._rightThumbResizer = e.NameScope.Find<Thumb>("PART_RightThumbResizer");
            this._contentControl = e.NameScope.Find<ContentControl>("PART_ContentControl");
            this._rightThumbResizer.DragDelta += ResizerDragDelta;
            this._rightThumbResizer.DragStarted += ResizerDragStarted;

            var content = (Content as XDataGridHeaderDescriptor);

            this._contentControl.WhenAnyValue(x => x.Width)
                                .DistinctUntilChanged()
                                .Throttle(TimeSpan.FromSeconds(1 / 24))
                                .ObserveOn(RxApp.MainThreadScheduler)
                                .Subscribe(x => content.HeaderWidth = x);

        }

        private void ResizerDragStarted(object sender, VectorEventArgs e)
        {
            if (Double.IsNaN(_contentControl.Width))
            {
                _contentControl.Width = _contentControl.Bounds.Width;
            }
        }

        private void ResizerDragDelta(object sender, VectorEventArgs e)
        {
            var newW = _contentControl.Width + e.Vector.X;

            if (newW <= 0) return;

            _contentControl.Width = newW;
        }
    }
}
