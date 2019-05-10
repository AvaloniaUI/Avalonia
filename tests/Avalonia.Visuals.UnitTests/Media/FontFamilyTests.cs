// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class FontFamilyTests
    {
        [Fact]
        public void Should_Implicitly_Convert_String_To_FontFamily()
        {
            FontFamily fontFamily = "Arial";

            Assert.Equal(new FontFamily("Arial"), fontFamily);
        }

        [Fact]
        public void Should_Be_Equal()
        {
            var fontFamily = new FontFamily("Arial");

            Assert.Equal(new FontFamily("Arial"), fontFamily);
        }

        [Fact]
        public void Should_Parse_FontFamily_With_SystemFont_Name()
        {
            var fontFamily = FontFamily.Parse("Courier New");

            Assert.Equal("Courier New", fontFamily.Name);
        }

        [Fact]
        public void Should_Parse_FontFamily_With_Fallbacks()
        {
            var fontFamily = FontFamily.Parse("Courier New, Times New Roman");

            Assert.Equal("Courier New", fontFamily.Name);

            Assert.Equal(2, fontFamily.FamilyNames.Count());

            Assert.Equal("Times New Roman", fontFamily.FamilyNames.Last());
        }

        [Fact]
        public void Should_Parse_FontFamily_With_Resource_Folder()
        {
            var source = new Uri("resm:Avalonia.Visuals.UnitTests#MyFont");

            var key = new FontFamilyKey(source);

            var fontFamily = FontFamily.Parse(source.OriginalString);

            Assert.Equal("MyFont", fontFamily.Name);

            Assert.Equal(key, fontFamily.Key);
        }

        [Fact]
        public void Should_Parse_FontFamily_With_Resource_Filename()
        {
            var source = new Uri("resm:Avalonia.Visuals.UnitTests.MyFont.ttf#MyFont");

            var key = new FontFamilyKey(source);

            var fontFamily = FontFamily.Parse(source.OriginalString);

            Assert.Equal("MyFont", fontFamily.Name);

            Assert.Equal(key, fontFamily.Key);
        }

        [Theory]
        [InlineData("resm:Avalonia.Visuals.UnitTests/Assets/Fonts#MyFont")]
        [InlineData("avares://Avalonia.Visuals.UnitTests/Assets/Fonts#MyFont")]
        public void Should_Create_FontFamily_From_Uri(string name)
        {
            var fontFamily = new FontFamily(name);

            Assert.Equal("MyFont", fontFamily.Name);

            Assert.NotNull(fontFamily.Key);
        }

        [Theory]
        [InlineData("resm:Avalonia.Visuals.UnitTests.Assets.Fonts", "#MyFont")]
        [InlineData("avares://Avalonia.Visuals.UnitTests/Assets/Fonts", "#MyFont")]
        [InlineData("avares://Avalonia.Visuals.UnitTests", "/Assets/Fonts#MyFont")]
        public void Should_Create_FontFamily_From_Uri_With_Base_Uri(string @base, string name)
        {
            var baseUri = new Uri(@base);

            var fontFamily = new FontFamily(baseUri, name);

            Assert.Equal("MyFont", fontFamily.Name);

            Assert.NotNull(fontFamily.Key);
        }
    }
}
