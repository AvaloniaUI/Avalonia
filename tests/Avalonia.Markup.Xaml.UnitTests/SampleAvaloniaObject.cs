using System;

namespace Avalonia.Markup.Xaml.UnitTests
{
    internal class SampleAvaloniaObject : AvaloniaObject
    {
        public static readonly StyledProperty<string> StringProperty =
            AvaloniaProperty.Register<AvaloniaObject, string>("StrProp", string.Empty);

        public static readonly StyledProperty<int> IntProperty =
            AvaloniaProperty.Register<AvaloniaObject, int>("IntProp");

        public int Int
        {
            get => GetValue(IntProperty);
            set => SetValue(IntProperty, value);
        }

        public string String
        {
            get => GetValue(StringProperty);
            set => SetValue(StringProperty, value);
        }
    }
}
