using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;

namespace Avalonia.Markup.Xaml.UnitTests.TypeConverter
{
    public class TestAvaloniaElement : AvaloniaObject
    {
        public static readonly StyledProperty<IBrush> TestBrushrProperty =
            AvaloniaProperty.Register<TestAvaloniaElement, IBrush>(nameof(TestBrush), null);

        public static readonly StyledProperty<Color> TestColorProperty =
            AvaloniaProperty.Register<TestAvaloniaElement, Color>(nameof(TestColor), default(Color));

        public IBrush TestBrush { get { return GetValue(TestBrushrProperty); } set { SetValue(TestBrushrProperty, value); } }

        public Color TestColor { get { return GetValue(TestColorProperty); } set { SetValue(TestColorProperty, value); } }
    }
}
