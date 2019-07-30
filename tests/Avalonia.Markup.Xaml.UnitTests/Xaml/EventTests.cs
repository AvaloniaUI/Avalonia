// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class EventTests : XamlTestBase
    {
        [Fact]
        public void Event_Is_Attached()
        {
            var xaml = @"<Button xmlns='https://github.com/avaloniaui' Click='OnClick'/>";
            var loader = new AvaloniaXamlLoader();
            var target = new MyButton();

            loader.Load(xaml, rootInstance: target);
            RaiseClick(target);

            Assert.True(target.Clicked);
        }

        [Fact]
        public void Exception_Is_Thrown_If_Event_Not_Found()
        {
            var xaml = @"<Button xmlns='https://github.com/avaloniaui' Click='NotFound'/>";
            var loader = new AvaloniaXamlLoader();
            var target = new MyButton();

            XamlTestHelpers.AssertThrowsXamlException(() => loader.Load(xaml, rootInstance: target));
        }

        private void RaiseClick(MyButton target)
        {
            target.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = Button.KeyDownEvent,
                Key = Key.Enter,
            });
        }

        public class MyButton : Button
        {
            public bool Clicked { get; private set; }

            public void OnClick(object sender, RoutedEventArgs e)
            {
                Clicked = true;
            }
        }
    }
}
