// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Controls;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class StylesTests
    {
        [Fact]
        public void Adding_Style_With_Resources_Should_Raise_ResourceChanged()
        {
            var style = new Style
            {
                Resources = { { "foo", "bar" } },
            };

            var target = new Styles();
            var raised = false;

            target.ResourcesChanged += (_, __) => raised = true;
            target.Add(style);

            Assert.True(raised);
        }

        [Fact]
        public void Removing_Style_With_Resources_Should_Raise_ResourceChanged()
        {
            var target = new Styles
            {
                new Style
                {
                    Resources = { { "foo", "bar" } },
                }
            };

            var raised = false;

            target.ResourcesChanged += (_, __) => raised = true;
            target.Clear();

            Assert.True(raised);
        }

        [Fact]
        public void Adding_Style_Without_Resources_Should_Not_Raise_ResourceChanged()
        {
            var style = new Style();
            var target = new Styles();
            var raised = false;

            target.ResourcesChanged += (_, __) => raised = true;
            target.Add(style);

            Assert.False(raised);
        }

        [Fact]
        public void Adding_Resource_Should_Raise_Child_ResourceChanged()
        {
            Style child;
            var target = new Styles
            {
                (child = new Style()),
            };

            var raised = false;

            child.ResourcesChanged += (_, __) => raised = true;
            target.Resources.Add("foo", "bar");

            Assert.True(raised);
        }

        [Fact]
        public void Adding_Resource_To_Younger_Sibling_Style_Should_Raise_ResourceChanged()
        {
            Style style1;
            Style style2;
            var target = new Styles
            {
                (style1 = new Style()),
                (style2 = new Style()),
            };

            var raised = false;

            style2.ResourcesChanged += (_, __) => raised = true;
            style1.Resources.Add("foo", "bar");

            Assert.True(raised);
        }

        [Fact]
        public void Adding_Resource_To_Older_Sibling_Style_Should_Raise_ResourceChanged()
        {
            Style style1;
            Style style2;
            var target = new Styles
            {
                (style1 = new Style()),
                (style2 = new Style()),
            };

            var raised = false;

            style1.ResourcesChanged += (_, __) => raised = true;
            style2.Resources.Add("foo", "bar");

            Assert.False(raised);
        }


        [Fact]
        public void Finds_Resource_In_Merged_Dictionary()
        {
            var target = new Styles
            {
                Resources = new ResourceDictionary
                {
                    MergedDictionaries =
                    {
                        new ResourceDictionary
                        {
                            { "foo", "bar" },
                        }
                    }
                }
            };

            var result = target.FindResource("foo");

            Assert.Equal("bar", result);
        }
    }
}
