// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Markup.Xaml.Templates;
using System;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Templates
{
    public class MemberSelectorTests
    {
        [Fact]
        public void Should_Select_Child_Property_Value()
        {
            var selector = new MemberSelector() { MemberName = "Child.StringValue" };

            var data = new Item()
            {
                Child = new Item() { StringValue = "Value1" }
            };

            Assert.Same("Value1", selector.Select(data));
        }

        [Fact]
        public void Should_Select_Child_Property_Value_In_Multiple_Items()
        {
            var selector = new MemberSelector() { MemberName = "Child.StringValue" };

            var data = new Item[]
            {
                new Item() { Child = new Item() { StringValue = "Value1" } },
                new Item() { Child = new Item() { StringValue = "Value2" } },
                new Item() { Child = new Item() { StringValue = "Value3" } }
            };

            Assert.Same("Value1", selector.Select(data[0]));
            Assert.Same("Value2", selector.Select(data[1]));
            Assert.Same("Value3", selector.Select(data[2]));
        }

        [Fact]
        public void Should_Select_MoreComplex_Property_Value()
        {
            var selector = new MemberSelector() { MemberName = "Child.Child.Child.StringValue" };

            var data = new Item()
            {
                Child = new Item()
                {
                    Child = new Item()
                    {
                        Child = new Item() { StringValue = "Value1" }
                    }
                }
            };

            Assert.Same("Value1", selector.Select(data));
        }

        [Fact]
        public void Should_Select_Null_Value_On_Null_Object()
        {
            var selector = new MemberSelector() { MemberName = "StringValue" };

            Assert.Null(selector.Select(null));
        }

        [Fact]
        public void Should_Select_Null_Value_On_Wrong_MemberName()
        {
            var selector = new MemberSelector() { MemberName = "WrongProperty" };

            var data = new Item() { StringValue = "Value1" };

            Assert.Null(selector.Select(data));
        }

        [Fact]
        public void Should_Select_Simple_Property_Value()
        {
            var selector = new MemberSelector() { MemberName = "StringValue" };

            var data = new Item() { StringValue = "Value1" };

            Assert.Same("Value1", selector.Select(data));
        }

        [Fact]
        public void Should_Select_Simple_Property_Value_In_Multiple_Items()
        {
            var selector = new MemberSelector() { MemberName = "StringValue" };

            var data = new Item[]
            {
                new Item() { StringValue = "Value1" },
                new Item() { StringValue = "Value2" },
                new Item() { StringValue = "Value3" }
            };

            Assert.Same("Value1", selector.Select(data[0]));
            Assert.Same("Value2", selector.Select(data[1]));
            Assert.Same("Value3", selector.Select(data[2]));
        }

        [Fact]
        public void Should_Select_Target_On_Empty_MemberName()
        {
            var selector = new MemberSelector();

            var data = new Item() { StringValue = "Value1" };

            Assert.Same(data, selector.Select(data));
        }

        [Fact]
        public void Should_Support_Change_Of_MemberName()
        {
            var selector = new MemberSelector() { MemberName = "StringValue" };

            var data = new Item()
            {
                StringValue = "Value1",
                IntValue = 1
            };

            Assert.Same("Value1", selector.Select(data));

            selector.MemberName = "IntValue";

            Assert.Equal(1, selector.Select(data));
        }

        [Fact]
        public void Should_Support_Change_Of_Target_Value()
        {
            var selector = new MemberSelector() { MemberName = "StringValue" };

            var data = new Item()
            {
                StringValue = "Value1"
            };

            Assert.Same("Value1", selector.Select(data));

            data.StringValue = "Value2";

            Assert.Same("Value2", selector.Select(data));
        }

        private class Item
        {
            public Item Child { get; set; }
            public int IntValue { get; set; }

            public string StringValue { get; set; }
        }
    }
}