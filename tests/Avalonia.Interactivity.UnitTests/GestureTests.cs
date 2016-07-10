// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Xunit;

namespace Avalonia.Interactivity.UnitTests
{
    public class GestureTests
    {
        [Fact]
        public void Tapped_Should_Follow_Pointer_Pressed_Released()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var result = new List<string>();

            decorator.AddHandler(Border.PointerPressedEvent, (s, e) => result.Add("dp"));
            decorator.AddHandler(Border.PointerReleasedEvent, (s, e) => result.Add("dr"));
            decorator.AddHandler(Gestures.TappedEvent, (s, e) => result.Add("dt"));
            border.AddHandler(Border.PointerPressedEvent, (s, e) => result.Add("bp"));
            border.AddHandler(Border.PointerReleasedEvent, (s, e) => result.Add("br"));
            border.AddHandler(Gestures.TappedEvent, (s, e) => result.Add("bt"));

            border.RaiseEvent(new PointerPressedEventArgs());
            border.RaiseEvent(new PointerReleasedEventArgs());

            Assert.Equal(new[] { "bp", "dp", "br", "dr", "bt", "dt" }, result);
        }

        [Fact]
        public void Tapped_Should_Be_Raised_Even_When_PointerPressed_Handled()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var result = new List<string>();

            border.AddHandler(Border.PointerPressedEvent, (s, e) => e.Handled = true);
            decorator.AddHandler(Gestures.TappedEvent, (s, e) => result.Add("dt"));
            border.AddHandler(Gestures.TappedEvent, (s, e) => result.Add("bt"));

            border.RaiseEvent(new PointerPressedEventArgs());
            border.RaiseEvent(new PointerReleasedEventArgs());

            Assert.Equal(new[] { "bt", "dt" }, result);
        }

        [Fact]
        public void DoubleTapped_Should_Follow_Pointer_Pressed_Released_Pressed()
        {
            Border border = new Border();
            var decorator = new Decorator
            {
                Child = border
            };
            var result = new List<string>();

            decorator.AddHandler(Border.PointerPressedEvent, (s, e) => result.Add("dp"));
            decorator.AddHandler(Border.PointerReleasedEvent, (s, e) => result.Add("dr"));
            decorator.AddHandler(Gestures.TappedEvent, (s, e) => result.Add("dt"));
            decorator.AddHandler(Gestures.DoubleTappedEvent, (s, e) => result.Add("ddt"));
            border.AddHandler(Border.PointerPressedEvent, (s, e) => result.Add("bp"));
            border.AddHandler(Border.PointerReleasedEvent, (s, e) => result.Add("br"));
            border.AddHandler(Gestures.TappedEvent, (s, e) => result.Add("bt"));
            border.AddHandler(Gestures.DoubleTappedEvent, (s, e) => result.Add("bdt"));

            border.RaiseEvent(new PointerPressedEventArgs());
            border.RaiseEvent(new PointerReleasedEventArgs());
            border.RaiseEvent(new PointerPressedEventArgs { ClickCount = 2 });

            Assert.Equal(new[] { "bp", "dp", "br", "dr", "bt", "dt", "bp", "dp", "bdt", "ddt" }, result);
        }
    }
}
