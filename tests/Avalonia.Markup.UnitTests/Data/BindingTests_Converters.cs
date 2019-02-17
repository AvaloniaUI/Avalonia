// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class BindingTests_Converters
    {
        [Fact]
        public void Converter_Should_Be_Used()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var target = new Binding(nameof(Class1.Foo))
            {
                Converter = StringConverters.IsNullOrEmpty,
            };

            var expressionObserver = (BindingExpression)target.Initiate(
                textBlock, 
                TextBlock.TextProperty).Observable;

            Assert.Same(StringConverters.IsNullOrEmpty, expressionObserver.Converter);
        }

        public class When_Binding_To_String
        {
            [Fact]
            public void StringFormatConverter_Should_Be_Used_When_Binding_Has_StringFormat()
            {
                var textBlock = new TextBlock
                {
                    DataContext = new Class1(),
                };

                var target = new Binding(nameof(Class1.Foo))
                {
                    StringFormat = "Hello {0}",
                };

                var expressionObserver = (BindingExpression)target.Initiate(
                    textBlock,
                    TextBlock.TextProperty).Observable;

                Assert.IsType<StringFormatValueConverter>(expressionObserver.Converter);
            }
        }

        public class When_Binding_To_Object
        {
            [Fact]
            public void StringFormatConverter_Should_Be_Used_When_Binding_Has_StringFormat()
            {
                var textBlock = new TextBlock
                {
                    DataContext = new Class1(),
                };

                var target = new Binding(nameof(Class1.Foo))
                {
                    StringFormat = "Hello {0}",
                };

                var expressionObserver = (BindingExpression)target.Initiate(
                    textBlock,
                    TextBlock.TagProperty).Observable;

                Assert.IsType<StringFormatValueConverter>(expressionObserver.Converter);
            }
        }

        public class When_Binding_To_Non_String_Or_Object
        {
            [Fact]
            public void StringFormatConverter_Should_Not_Be_Used_When_Binding_Has_StringFormat()
            {
                var textBlock = new TextBlock
                {
                    DataContext = new Class1(),
                };

                var target = new Binding(nameof(Class1.Foo))
                {
                    StringFormat = "Hello {0}",
                };

                var expressionObserver = (BindingExpression)target.Initiate(
                    textBlock,
                    TextBlock.MarginProperty).Observable;

                Assert.Same(DefaultValueConverter.Instance, expressionObserver.Converter);
            }
        }

        [Fact]
        public void StringFormat_Should_Be_Applied()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var target = new Binding(nameof(Class1.Foo))
            {
                StringFormat = "Hello {0}",
            };

            textBlock.Bind(TextBlock.TextProperty, target);

            Assert.Equal("Hello foo", textBlock.Text);
        }

        [Fact]
        public void StringFormat_Should_Be_Applied_After_Converter()
        {
            var textBlock = new TextBlock
            {
                DataContext = new Class1(),
            };

            var target = new Binding(nameof(Class1.Foo))
            {
                Converter = StringConverters.IsNotNullOrEmpty,
                StringFormat = "Hello {0}",
            };

            textBlock.Bind(TextBlock.TextProperty, target);

            Assert.Equal("Hello True", textBlock.Text);
        }

        private class Class1
        {
            public string Foo { get; set; } = "foo";
        }
    }
}
