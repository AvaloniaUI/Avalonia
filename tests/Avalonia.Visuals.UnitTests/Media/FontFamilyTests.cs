// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media
{
    public class FontFamilyTests
    {
        [Fact]
        public void Exception_Should_Be_Thrown_If_Name_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new FontFamily((string)null));
        }

        [Fact]
        public void Exception_Should_Be_Thrown_If_Names_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new FontFamily((IEnumerable<string>)null));
        }

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
        public void Parse_Parses_FontFamily_With_Name()
        {
            var fontFamily = FontFamily.Parse("Courier New");

            Assert.Equal("Courier New", fontFamily.Name);
        }

        [Fact]
        public void Parse_Parses_FontFamily_With_Names()
        {
            var fontFamily = FontFamily.Parse("Courier New, Times New Roman");

            Assert.Equal("Courier New", fontFamily.Name);

            Assert.Equal(2, fontFamily.FamilyNames.Count());

            Assert.Equal("Times New Roman", fontFamily.FamilyNames.Last());
        }

        [Fact]
        public void Parse_Parses_FontFamily_With_Resource_Folder()
        {
            var source = new Uri("resm:Avalonia.Visuals.UnitTests#MyFont");

            var key = new FontFamilyKey(source);

            var fontFamily = FontFamily.Parse(source.OriginalString);

            Assert.Equal("MyFont", fontFamily.Name);

            Assert.Equal(key, fontFamily.Key);
        }

        [Fact]
        public void Parse_Parses_FontFamily_With_Resource_Filename()
        {
            var source = new Uri("resm:Avalonia.Visuals.UnitTests.MyFont.ttf#MyFont");

            var key = new FontFamilyKey(source);

            var fontFamily = FontFamily.Parse(source.OriginalString);

            Assert.Equal("MyFont", fontFamily.Name);

            Assert.Equal(key, fontFamily.Key);
        }
    }
}
