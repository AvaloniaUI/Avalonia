// -----------------------------------------------------------------------
// <copyright file="SelectingItemsControlTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using Perspex.Controls.Primitives;
    using Perspex.Input;
    using Perspex.Interactivity;
    using Xunit;

    public class SelectingItemsControlTests
    {
        [Fact]
        public void PointerPressed_Event_Should_Be_Handled()
        {
            var target = new SelectingItemsControl();

            var e = new PointerPressEventArgs
            {
                RoutedEvent = InputElement.PointerPressedEvent
            };

            target.RaiseEvent(e);

            Assert.True(e.Handled);
        }
    }
}
