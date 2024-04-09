using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class EventTests : XamlTestBase
    {
        [Fact]
        public void Event_Is_Assigned()
        {
            var xaml = @"<Button xmlns='https://github.com/avaloniaui' Click='OnClick'/>";
            var target = new MyButton();

            AvaloniaRuntimeXamlLoader.Load(xaml, rootInstance: target);

            target.RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = Button.ClickEvent,
            });

            Assert.True(target.WasClicked);
        }

        [Fact]
        public void Attached_Event_Is_Assigned()
        {
            var xaml = @"<Button xmlns='https://github.com/avaloniaui' Gestures.Tapped='OnTapped'/>";
            var target = new MyButton();

            AvaloniaRuntimeXamlLoader.Load(xaml, rootInstance: target);

            target.RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = Gestures.TappedEvent,
            });

            Assert.True(target.WasTapped);
        }

        [Fact]
        public void Attached_Event_Is_Assigned_Generic()
        {
            var xaml = @"<Panel xmlns='https://github.com/avaloniaui'><Grid DoubleTapped='OnTapped'><Button Name='target'/></Grid></Panel>";
            var host = new MyPanel();

            AvaloniaRuntimeXamlLoader.Load(xaml, rootInstance: host);

            var target = host.FindControl<Button>("target");

            Assert.NotNull(target);

            target.RaiseEvent(new TappedEventArgs(Gestures.DoubleTappedEvent, default));

            Assert.True(host.WasTapped);
        }

        [Fact]
        public void Exception_Is_Thrown_If_Event_Not_Found()
        {
            var xaml = @"<Button xmlns='https://github.com/avaloniaui' Click='NotFound'/>";
            var target = new MyButton();

            XamlTestHelpers.AssertThrowsXamlException(() => AvaloniaRuntimeXamlLoader.Load(xaml, rootInstance: target));
        }



        [Fact]
        public void Attached_Event_Routed_Event_Handler()
        {
            var xaml = @"<Panel xmlns='https://github.com/avaloniaui' Button.Click='OnClick'><Button Name='target'/></Panel>";
            var host = new MyPanel();

            AvaloniaRuntimeXamlLoader.Load(xaml, rootInstance: host);

            var target = host.FindControl<Button>("target");
            target.RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = Button.ClickEvent,
            });

            Assert.True(host.WasClicked);
        }

        public class MyButton : Button
        {
            public bool WasClicked { get; private set; }
            public bool WasTapped { get; private set; }

            public void OnClick(object sender, RoutedEventArgs e) => WasClicked = true;
            public void OnTapped(object sender, RoutedEventArgs e) => WasTapped = true;
        }

        public class MyPanel : Panel
        {
            public bool WasClicked { get; private set; }
            public bool WasTapped { get; private set; }
            public void OnClick(object sender, RoutedEventArgs e) => WasClicked = true;
            public void OnTapped(object sender, RoutedEventArgs e) => WasTapped = true;
        }
    }
}
