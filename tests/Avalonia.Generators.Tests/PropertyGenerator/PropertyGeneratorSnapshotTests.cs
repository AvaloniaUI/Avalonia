using Xunit;
using static Avalonia.Generators.Tests.PropertyGenerator.PropertyGeneratorTestHelper;

namespace Avalonia.Generators.Tests.PropertyGenerator;

public class PropertyGeneratorSnapshotTests
{
    [Fact]
    public void Styled_Basic() => AssertGeneratedCode("Styled_Basic", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty]
            public partial string? Header { get; set; }
        }
        """,
        expectedHintName: "TestNs.MyControl.AvaloniaProperties.g.cs");

    [Fact]
    public void Styled_ConstDefault() => AssertGeneratedCode("Styled_ConstDefault", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty(DefaultValue = 100)]
            public partial int Width { get; set; }
        }
        """);

    [Fact]
    public void Styled_ConstDefaultNeedsCast() => AssertGeneratedCode("Styled_ConstDefaultNeedsCast", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty(DefaultValue = 100)]
            public partial double Width { get; set; }

            [StyledProperty(DefaultValue = double.NaN)]
            public partial double Height { get; set; }

            [StyledProperty(DefaultValue = "")]
            public partial object? Tag { get; set; }
        }
        """);

    [Fact]
    public void Styled_StaticCtorCoexists() => AssertGeneratedCode("Styled_StaticCtorCoexists", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            static MyControl()
            {
                PaddingProperty.OverrideDefaultValue<MyControl>(new Thickness(4));
            }

            [StyledProperty]
            public partial Thickness Padding { get; set; }
        }
        """);

    [Fact]
    public void Styled_InheritsBindingMode() => AssertGeneratedCode("Styled_InheritsBindingMode", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty(Inherits = true, DefaultBindingMode = BindingMode.TwoWay)]
            public partial double FontSize { get; set; }
        }
        """);

    [Fact]
    public void Styled_Changed() => AssertGeneratedCode("Styled_Changed", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty(ChangedMethodName = nameof(OnIsOpenChanged))]
            public partial bool IsOpen { get; set; }

            private partial void OnIsOpenChanged(bool oldValue, bool newValue)
            {
            }
        }
        """);

    [Fact]
    public void Styled_ValidateCoerce() => AssertGeneratedCode("Styled_ValidateCoerce", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty(ValidateMethodName = nameof(ValidateValue), CoerceMethodName = nameof(CoerceValue), DefaultValue = 0)]
            public partial int Value { get; set; }

            private static partial bool ValidateValue(int value) => value >= 0;

            private static partial int CoerceValue(AvaloniaObject sender, int value) => value > 100 ? 100 : value;
        }
        """);

    [Fact]
    public void Styled_AddOwner() => AssertGeneratedCode("Styled_AddOwner", """
        namespace TestNs;

        public class RangeBase : AvaloniaObject
        {
            public static readonly StyledProperty<double> ValueProperty =
                AvaloniaProperty.Register<RangeBase, double>(nameof(Value));

            public double Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }
        }

        public partial class MyControl : RangeBase
        {
            [StyledProperty(AddOwnerFrom = typeof(RangeBase))]
            public new partial double Value { get; set; }
        }
        """);

    [Fact]
    public void Styled_AddOwnerOverrides() => AssertGeneratedCode("Styled_AddOwnerOverrides", """
        namespace TestNs;

        public class RangeBase : AvaloniaObject
        {
            public static readonly StyledProperty<double> ValueProperty =
                AvaloniaProperty.Register<RangeBase, double>(nameof(Value));

            public double Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }
        }

        public partial class MyControl : RangeBase
        {
            [StyledProperty(AddOwnerFrom = typeof(RangeBase), DefaultValue = 1.0, CoerceMethodName = nameof(CoerceValue), EnableDataValidation = true)]
            public new partial double Value { get; set; }

            private static partial double CoerceValue(AvaloniaObject sender, double value) => value < 0 ? 0 : value;
        }
        """);

    [Fact]
    public void Styled_NonPublicSetter() => AssertGeneratedCode("Styled_NonPublicSetter", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty]
            public partial bool IsPressed { get; private set; }
        }
        """);

    [Fact]
    public void Direct_Basic() => AssertGeneratedCode("Direct_Basic", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [DirectProperty]
            public partial string Text { get; set; } = "";
        }
        """);

    [Fact]
    public void Direct_RefTypeInitializer() => AssertGeneratedCode("Direct_RefTypeInitializer", """
        using System.Collections;
        using Avalonia.Collections;

        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [DirectProperty]
            public partial IEnumerable? Items { get; set; } = new AvaloniaList<object>();
        }
        """);

    [Fact]
    public void Direct_ReadOnly() => AssertGeneratedCode("Direct_ReadOnly", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [DirectProperty]
            public partial int SelectedIndex { get; private set; } = -1;

            public void Select(int index) => SelectedIndex = index;
        }
        """);

    [Fact]
    public void Direct_UnsetChanged() => AssertGeneratedCode("Direct_UnsetChanged", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [DirectProperty(UnsetValue = -1, ChangedMethodName = nameof(OnCountChanged))]
            public partial int Count { get; set; } = -1;

            private partial void OnCountChanged(int oldValue, int newValue)
            {
            }
        }
        """);

    [Fact]
    public void Direct_AddOwner() => AssertGeneratedCode("Direct_AddOwner", """
        namespace TestNs;

        public class TextBase : AvaloniaObject
        {
            public static readonly DirectProperty<TextBase, string> TextProperty =
                AvaloniaProperty.RegisterDirect<TextBase, string>(nameof(Text), static o => o.Text, static (o, v) => o.Text = v);

            private string _text = "";

            public string Text
            {
                get => _text;
                set => SetAndRaise(TextProperty, ref _text, value);
            }
        }

        public partial class MyControl : AvaloniaObject
        {
            [DirectProperty(AddOwnerFrom = typeof(TextBase))]
            public partial string Text { get; set; } = "";
        }
        """);

    [Fact]
    public void Attached_Basic() => AssertGeneratedCode("Attached_Basic", """
        namespace TestNs;

        public partial class Grid : AvaloniaObject
        {
            [AttachedProperty]
            public static partial int GetRow(Visual element);
        }
        """);

    [Fact]
    public void Attached_DefaultInherits() => AssertGeneratedCode("Attached_DefaultInherits", """
        namespace TestNs;

        public partial class Grid : AvaloniaObject
        {
            [AttachedProperty(DefaultValue = 1)]
            public static partial int GetRowSpan(Visual element);

            [AttachedProperty(Inherits = true)]
            public static partial double GetFontSize(Visual element);
        }
        """);

    [Fact]
    public void Attached_Changed() => AssertGeneratedCode("Attached_Changed", """
        namespace TestNs;

        public partial class DockPanel : AvaloniaObject
        {
            [AttachedProperty(ChangedMethodName = nameof(OnDockChanged))]
            public static partial int GetDock(Visual element);

            private static partial void OnDockChanged(Visual host, int oldValue, int newValue)
            {
            }
        }
        """);

    [Fact]
    public void Attached_ValidateCoerce() => AssertGeneratedCode("Attached_ValidateCoerce", """
        namespace TestNs;

        public partial class Grid : AvaloniaObject
        {
            [AttachedProperty(ValidateMethodName = nameof(ValidateOrder), CoerceMethodName = nameof(CoerceOrder), DefaultValue = 0)]
            public static partial int GetOrder(Visual element);

            private static partial bool ValidateOrder(int value) => value >= 0;

            private static partial int CoerceOrder(AvaloniaObject sender, int value) => value < 0 ? 0 : value;
        }
        """);

    [Fact]
    public void Attached_NonPublicAccessors() => AssertGeneratedCode("Attached_NonPublicAccessors", """
        namespace TestNs;

        public partial class Host : AvaloniaObject
        {
            [AttachedProperty]
            internal static partial bool GetIsHosted(Visual element);
        }
        """);

    [Fact]
    public void Attached_Nullable() => AssertGeneratedCode("Attached_Nullable", """
        namespace TestNs;

        public partial class ToolTip : AvaloniaObject
        {
            [AttachedProperty]
            public static partial string? GetTip(Visual element);
        }
        """);

    [Fact]
    public void Attached_StaticOwner() => AssertGeneratedCode("Attached_StaticOwner", """
        namespace TestNs;

        public static partial class ScrollHelper
        {
            [AttachedProperty(DefaultValue = false)]
            public static partial bool GetIsScrollTarget(Visual element);
        }
        """,
        expectedHintName: "TestNs.ScrollHelper.AvaloniaProperties.g.cs");

    [Fact]
    public void Attached_AddOwner() => AssertGeneratedCode("Attached_AddOwner", """
        namespace TestNs;

        public class BasePanel : AvaloniaObject
        {
            public static readonly AttachedProperty<int> RowProperty =
                AvaloniaProperty.RegisterAttached<BasePanel, Visual, int>("Row");
        }

        public partial class MyPanel : BasePanel
        {
            [AttachedProperty(AddOwnerFrom = typeof(BasePanel), DefaultValue = 2)]
            public static partial int GetRow(Visual element);
        }
        """);

    [Fact]
    public void NestedOwner() => AssertGeneratedCode("NestedOwner", """
        namespace TestNs;

        public partial class Outer
        {
            public partial class MyControl : AvaloniaObject
            {
                [StyledProperty]
                public partial string? Header { get; set; }
            }
        }
        """,
        expectedHintName: "TestNs.Outer.MyControl.AvaloniaProperties.g.cs");

    [Fact]
    public void GlobalNamespace() => AssertGeneratedCode("GlobalNamespace", """
        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty]
            public partial string? Header { get; set; }
        }
        """,
        expectedHintName: "MyControl.AvaloniaProperties.g.cs");

    [Fact]
    public void GenericOwner() => AssertGeneratedCode("GenericOwner", """
        namespace TestNs;

        public partial class MyControl<T> : AvaloniaObject
            where T : class
        {
            [StyledProperty]
            public partial T? Item { get; set; }
        }
        """,
        expectedHintName: "TestNs.MyControl_1.AvaloniaProperties.g.cs");

    [Fact]
    public void MultiProperty() => AssertGeneratedCode("MultiProperty", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty(ChangedMethodName = nameof(OnFlagChanged))]
            public partial bool First { get; set; }

            [StyledProperty(ChangedMethodName = nameof(OnFlagChanged))]
            public partial bool Second { get; set; }

            [DirectProperty]
            public partial string Text { get; set; } = "";

            [AttachedProperty]
            public static partial int GetOrder(Visual element);

            private partial void OnFlagChanged(bool oldValue, bool newValue)
            {
            }
        }
        """);
}
