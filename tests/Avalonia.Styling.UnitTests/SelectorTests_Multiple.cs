// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class SelectorTests_Multiple
    {
        [Fact]
        public void Template_Child_Of_Control_With_Two_Classes()
        {
            var template = new FuncControlTemplate(parent =>
            {
                return new Border
                {
                    Name = "border",
                };
            });

            var control = new Button
            {
                Template = template,
            };

            control.ApplyTemplate();

            var selector = default(Selector)
                .OfType<Button>()
                .Class("foo")
                .Class("bar")
                .Template()
                .Name("border");

            var border = (Border)((IVisual)control).VisualChildren.Single();
            var values = new List<bool>();
            var activator = selector.Match(border).ObservableResult;

            activator.Subscribe(x => values.Add(x));

            Assert.Equal(new[] { false }, values);
            control.Classes.AddRange(new[] { "foo", "bar" });
            Assert.Equal(new[] { false, true }, values);
            control.Classes.Remove("foo");
            Assert.Equal(new[] { false, true, false }, values);
        }

        [Fact]
        public void TargetType_OfType()
        {
            var selector = default(Selector).OfType<Button>();

            Assert.Equal(typeof(Button), selector.TargetType);
        }

        [Fact]
        public void TargetType_OfType_Class()
        {
            var selector = default(Selector)
                .OfType<Button>()
                .Class("foo");

            Assert.Equal(typeof(Button), selector.TargetType);
        }

        [Fact]
        public void TargetType_Is_Class()
        {
            var selector = default(Selector)
                .Is<Button>()
                .Class("foo");

            Assert.Equal(typeof(Button), selector.TargetType);
        }

        [Fact]
        public void TargetType_Child()
        {
            var selector = default(Selector)
                .OfType<Button>()
                .Child()
                .OfType<TextBlock>();

            Assert.Equal(typeof(TextBlock), selector.TargetType);
        }

        [Fact]
        public void TargetType_Descendant()
        {
            var selector = default(Selector)
                .OfType<Button>()
                .Descendant()
                .OfType<TextBlock>();

            Assert.Equal(typeof(TextBlock), selector.TargetType);
        }

        [Fact]
        public void TargetType_Template()
        {
            var selector = default(Selector)
                .OfType<Button>()
                .Template()
                .OfType<TextBlock>();

            Assert.Equal(typeof(TextBlock), selector.TargetType);
        }
    }
}
