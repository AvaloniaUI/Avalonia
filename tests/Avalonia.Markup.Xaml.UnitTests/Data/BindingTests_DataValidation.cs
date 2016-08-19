// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Avalonia.Markup.Xaml.Data;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_DataValidation
    {
        [Fact]
        public void Initiate_Should_Not_Enable_Data_Validation_With_BindingPriority_LocalValue()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var target = new Binding(nameof(Class1.Foo));
            var instanced = target.Initiate(textBlock, TextBlock.TextProperty, enableDataValidation: false);
            var subject = (BindingExpression)instanced.Subject;
            object result = null;

            subject.Subscribe(x => result = x);

            Assert.IsType<string>(result);
        }

        [Fact]
        public void Initiate_Should_Enable_Data_Validation_With_BindingPriority_LocalValue()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var target = new Binding(nameof(Class1.Foo));
            var instanced = target.Initiate(textBlock, TextBlock.TextProperty, enableDataValidation: true);
            var subject = (BindingExpression)instanced.Subject;
            object result = null;

            subject.Subscribe(x => result = x);

            Assert.Equal(new BindingNotification("foo"), result);
        }

        [Fact]
        public void Initiate_Should_Not_Enable_Data_Validation_With_BindingPriority_TemplatedParent()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var target = new Binding(nameof(Class1.Foo)) { Priority = BindingPriority.TemplatedParent };
            var instanced = target.Initiate(textBlock, TextBlock.TextProperty, enableDataValidation: true);
            var subject = (BindingExpression)instanced.Subject;
            object result = null;

            subject.Subscribe(x => result = x);

            Assert.IsType<string>(result);
        }

        private class Class1
        {
            public string Foo { get; set; } = "foo";
        }
    }
}
