using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    
    public class SplitViewTests
    {
        [Fact]
        public void SplitView_PaneOpening_Should_Fire_Before_PaneOpened()
        {
            var splitView = new SplitView();

            bool handledOpening = false;
            splitView.PaneOpening += (x, e) =>
            {
                handledOpening = true;
            };

            splitView.PaneOpened += (x, e) =>
            {
                Assert.True(handledOpening);
            };

            splitView.IsPaneOpen = true;
        }

        [Fact]
        public void SplitView_PaneClosing_Should_Fire_Before_PaneClosed()
        {
            var splitView = new SplitView();
            splitView.IsPaneOpen = true;

            bool handledClosing = false;
            splitView.PaneClosing += (x, e) =>
            {
                handledClosing = true;
            };

            splitView.PaneClosed += (x, e) =>
            {
                Assert.True(handledClosing);
            };

            splitView.IsPaneOpen = false;
        }

        [Fact]
        public void SplitView_Cancel_Close_Should_Prevent_Pane_From_Closing()
        {
            var splitView = new SplitView();
            splitView.IsPaneOpen = true;

            splitView.PaneClosing += (x, e) =>
            {
                e.Cancel = true;
            };

            splitView.IsPaneOpen = false;

            Assert.True(splitView.IsPaneOpen);
        }
    }
}
