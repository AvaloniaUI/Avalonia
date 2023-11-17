using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class NonControl : AvaloniaObject
    {
        public static readonly StyledProperty<Control> ControlProperty =
            AvaloniaProperty.Register<NonControl, Control>(nameof(Control));

        public static readonly StyledProperty<string> StringProperty =
            AvaloniaProperty.Register<NonControl, string>(nameof(String));

        //No getter or setter Avalonia property
        public static readonly StyledProperty<int> FooProperty =
            AvaloniaProperty.Register<NonControl, int>("Foo");

        //getter only Avalonia property
        public static readonly StyledProperty<string> BarProperty =
                AvaloniaProperty.Register<NonControl, string>(nameof(Bar));

        public Control Control
        {
            get => GetValue(ControlProperty);
            set => SetValue(ControlProperty, value);
        }

        public string String
        {
            get => GetValue(StringProperty);
            set => SetValue(StringProperty, value);
        }

        public string Bar => GetValue(BarProperty);
    }
}