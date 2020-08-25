using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.TypeConverter
{
    public class AssignmentTests : XamlTestBase
    {
        [Fact]
        public void Brush_Property_Set_Using_Name()
        {
            var testClass = typeof(TestAvaloniaElement);
            var parsed = AvaloniaRuntimeXamlLoader.Parse<TestAvaloniaElement>(
                $"<{testClass.Name} xmlns='clr-namespace:{testClass.Namespace}' TestBrush='AliceBlue'/>", testClass.Assembly);

            Assert.NotNull(parsed);
            Assert.NotNull(parsed.TestBrush);
            Assert.IsType(typeof(ImmutableSolidColorBrush), parsed.TestBrush);
            Assert.Equal("AliceBlue", ((ImmutableSolidColorBrush)parsed.TestBrush).Color.ToString());
            Assert.Equal(Color.Parse("AliceBlue"), ((ImmutableSolidColorBrush)parsed.TestBrush).Color);
        }

        [Fact]
        public void Color_Property_Set_Using_Name()
        {
            var testClass = typeof(TestAvaloniaElement);
            var parsed = AvaloniaRuntimeXamlLoader.Parse<TestAvaloniaElement>(
                $"<{testClass.Name} xmlns='clr-namespace:{testClass.Namespace}' TestColor='AliceBlue'/>", testClass.Assembly);

            Assert.NotNull(parsed);
            Assert.NotNull(parsed.TestColor);
            Assert.IsType(typeof(Color), parsed.TestColor);
            Assert.Equal("AliceBlue", (parsed.TestColor).ToString());
            Assert.Equal(Color.Parse("AliceBlue"), parsed.TestColor);
        }

        [Fact]
        public void Color_Property_Set_Using_HexStr()
        {
            var testClass = typeof(TestAvaloniaElement);
            var parsed = AvaloniaRuntimeXamlLoader.Parse<TestAvaloniaElement>(
                $"<{testClass.Name} xmlns='clr-namespace:{testClass.Namespace}' TestColor='#44556677'/>", testClass.Assembly);

            Assert.NotNull(parsed);
            Assert.NotNull(parsed.TestColor);
            Assert.IsType(typeof(Color), parsed.TestColor);
            Assert.Equal(Color.FromArgb(0x44,0x55,0x66,0x77), parsed.TestColor);
        }
    }
}
