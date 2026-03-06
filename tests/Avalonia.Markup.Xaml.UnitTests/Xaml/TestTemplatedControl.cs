using Avalonia.Controls.Primitives;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class TestTemplatedControl : TemplatedControl
    {
        public static readonly StyledProperty<object> TestDataProperty =
            AvaloniaProperty.Register<TestTemplatedControl, object>(nameof(TestData));

        public object TestData
        {
            get => GetValue(TestDataProperty);
            set => SetValue(TestDataProperty, value);
        }
    }
}
