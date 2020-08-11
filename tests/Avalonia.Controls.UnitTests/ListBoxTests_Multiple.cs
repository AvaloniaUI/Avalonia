using System.Linq;
using Avalonia.Input;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public partial class ListBoxTests
    {
        [Fact]
        public void Focusing_Item_With_Shift_And_Arrow_Key_Should_Add_To_Selection()
        {
            using var app = Start();
            
            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Multiple
            };

            Prepare(target);

            target.SelectedItem = "Foo";

            target.Presenter.RealizedElements.ElementAt(1).RaiseEvent(new GotFocusEventArgs
            {
                RoutedEvent = InputElement.GotFocusEvent,
                NavigationMethod = NavigationMethod.Directional,
                KeyModifiers = KeyModifiers.Shift
            });

            Assert.Equal(new[] { "Foo", "Bar" }, target.SelectedItems);
        }

        [Fact]
        public void Focusing_Item_With_Ctrl_And_Arrow_Key_Should_Add_To_Selection()
        {
            using var app = Start();
            
            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Multiple
            };

            Prepare(target);

            target.SelectedItem = "Foo";

            target.Presenter.RealizedElements.ElementAt(1).RaiseEvent(new GotFocusEventArgs
            {
                RoutedEvent = InputElement.GotFocusEvent,
                NavigationMethod = NavigationMethod.Directional,
                KeyModifiers = KeyModifiers.Control
            });

            Assert.Equal(new[] { "Foo", "Bar" }, target.SelectedItems);
        }

        [Fact]
        public void Focusing_Selected_Item_With_Ctrl_And_Arrow_Key_Should_Remove_From_Selection()
        {
            using var app = Start();
            
            var target = new ListBox
            {
                Items = new[] { "Foo", "Bar", "Baz " },
                SelectionMode = SelectionMode.Multiple
            };

            Prepare(target);

            target.SelectedItems.Add("Foo");
            target.SelectedItems.Add("Bar");

            target.Presenter.RealizedElements.ElementAt(0).RaiseEvent(new GotFocusEventArgs
            {
                RoutedEvent = InputElement.GotFocusEvent,
                NavigationMethod = NavigationMethod.Directional,
                KeyModifiers = KeyModifiers.Control
            });

            Assert.Equal(new[] { "Bar" }, target.SelectedItems);
        }
    }
}
