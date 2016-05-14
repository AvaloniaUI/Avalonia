using Avalonia.Media;
using System;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;

namespace Avalonia.Controls
{
    public class Thingamybob : Decorator
    {
        private ScrollViewer _scrollViewer;
        private ThingamybobPresenter _presenter;

        public override void ApplyTemplate()
        {
            if (Child == null)
            {
                _scrollViewer = new ScrollViewer();
                _presenter = new ThingamybobPresenter();
                _scrollViewer.Content = _presenter;

                Child = _scrollViewer;
            }
        }
    }
}
