// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
            var loader = new AvaloniaXamlLoader();
            var target = new MyButton();

            loader.Load(xaml, rootInstance: target);

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
            var loader = new AvaloniaXamlLoader();
            var target = new MyButton();

            loader.Load(xaml, rootInstance: target);

            target.RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = Gestures.TappedEvent,
            });

            Assert.True(target.WasTapped);
        }

        [Fact]
        public void Exception_Is_Thrown_If_Event_Not_Found()
        {
            var xaml = @"<Button xmlns='https://github.com/avaloniaui' Click='NotFound'/>";
            var loader = new AvaloniaXamlLoader();
            var target = new MyButton();

            XamlTestHelpers.AssertThrowsXamlException(() => loader.Load(xaml, rootInstance: target));
        }

        public class MyButton : Button
        {
            public bool WasClicked { get; private set; }
            public bool WasTapped { get; private set; }

            public void OnClick(object sender, RoutedEventArgs e) => WasClicked = true;
            public void OnTapped(object sender, RoutedEventArgs e) => WasTapped = true;
        }
    }
}
