using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
            [GeneratedStyledProperty]
            public partial string? Header { get; set; }
        }
        """,
        expectedHintName: "TestNs.MyControl.AvaloniaProperties.g.cs");

    [Fact]
    public void Styled_ConstDefault() => AssertGeneratedCode("Styled_ConstDefault", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty(DefaultValue = 100)]
            public partial int Width { get; set; }
        }
        """);

    [Fact]
    public void Styled_ConstDefaultNeedsCast() => AssertGeneratedCode("Styled_ConstDefaultNeedsCast", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty(DefaultValue = 100)]
            public partial double Width { get; set; }

            [GeneratedStyledProperty(DefaultValue = double.NaN)]
            public partial double Height { get; set; }

            [GeneratedStyledProperty(DefaultValue = "")]
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

            [GeneratedStyledProperty]
            public partial Thickness Padding { get; set; }
        }
        """);

    [Fact]
    public void Styled_InheritsBindingMode() => AssertGeneratedCode("Styled_InheritsBindingMode", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty(Inherits = true, DefaultBindingMode = BindingMode.TwoWay)]
            public partial double FontSize { get; set; }
        }
        """);

    [Fact]
    public void Styled_ValidateCoerce() => AssertGeneratedCode("Styled_ValidateCoerce", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty(ValidateMethodName = nameof(ValidateValue), CoerceMethodName = nameof(CoerceValue), DefaultValue = 0)]
            public partial int Value { get; set; }

            private static bool ValidateValue(int value) => value >= 0;

            private static int CoerceValue(AvaloniaObject sender, int value) => value > 100 ? 100 : value;
        }
        """);

    [Fact]
    public void Styled_SharedCallback() => AssertGeneratedCode("Styled_SharedCallback", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty(CoerceMethodName = nameof(CoerceValue))]
            public partial int First { get; set; }

            [GeneratedStyledProperty(CoerceMethodName = nameof(CoerceValue))]
            public partial int Second { get; set; }

            private static int CoerceValue(AvaloniaObject sender, int value) => value;
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
            [GeneratedStyledProperty(AddOwnerFrom = typeof(RangeBase))]
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
            [GeneratedStyledProperty(AddOwnerFrom = typeof(RangeBase), DefaultValue = 1.0, CoerceMethodName = nameof(CoerceValue), EnableDataValidation = true)]
            public new partial double Value { get; set; }

            private static double CoerceValue(AvaloniaObject sender, double value) => value < 0 ? 0 : value;
        }
        """);

    [Fact]
    public void Styled_AddOwnerFromGeneratedProperty() => AssertGeneratedCode("Styled_AddOwnerFromGeneratedProperty", """
        namespace TestNs;

        public partial class RangeBase : AvaloniaObject
        {
            // The AddOwner source is itself source-generated.
            [GeneratedStyledProperty]
            public partial double Value { get; set; }
        }

        public partial class MyControl : RangeBase
        {
            [GeneratedStyledProperty(AddOwnerFrom = typeof(RangeBase))]
            public new partial double Value { get; set; }
        }
        """,
        expectedHintName: "TestNs.MyControl.AvaloniaProperties.g.cs");

    [Fact]
    public void Styled_NonPublicSetter() => AssertGeneratedCode("Styled_NonPublicSetter", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty]
            public partial bool IsPressed { get; private set; }
        }
        """);

    [Fact]
    public void Direct_Basic() => AssertGeneratedCode("Direct_Basic", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedDirectProperty]
            public partial string Text { get; set; } = "";
        }
        """);

    // On C# 13 there is no field keyword, so direct properties get a named backing field instead.
    // An inline initializer (= expr) is not valid here and is intentionally omitted (see Direct_Basic).
    [Fact]
    public void Direct_Basic_CSharp13() => AssertGeneratedCode("Direct_Basic_CSharp13", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedDirectProperty]
            public partial string? Text { get; set; }
        }
        """, languageVersion: LanguageVersion.CSharp13);

    [Fact]
    public void Direct_ReadOnly_CSharp13() => AssertGeneratedCode("Direct_ReadOnly_CSharp13", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedDirectProperty]
            public partial int SelectedIndex { get; private set; }

            public void Select(int index) => SelectedIndex = index;
        }
        """, languageVersion: LanguageVersion.CSharp13);

    [Fact]
    public void Direct_RefTypeInitializer() => AssertGeneratedCode("Direct_RefTypeInitializer", """
        using System.Collections;
        using Avalonia.Collections;

        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedDirectProperty]
            public partial IEnumerable? Items { get; set; } = new AvaloniaList<object>();
        }
        """);

    [Fact]
    public void Direct_ReadOnly() => AssertGeneratedCode("Direct_ReadOnly", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedDirectProperty]
            public partial int SelectedIndex { get; private set; } = -1;

            public void Select(int index) => SelectedIndex = index;
        }
        """);

    [Fact]
    public void Direct_Unset() => AssertGeneratedCode("Direct_Unset", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedDirectProperty(UnsetValue = -1)]
            public partial int Count { get; set; } = -1;
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
            [GeneratedDirectProperty(AddOwnerFrom = typeof(TextBase))]
            public partial string Text { get; set; } = "";
        }
        """);

    [Fact]
    public void Attached_Basic() => AssertGeneratedCode("Attached_Basic", """
        namespace TestNs;

        public partial class Grid : AvaloniaObject
        {
            [GeneratedAttachedProperty]
            public static partial int GetRow(Visual element);
        }
        """);

    [Fact]
    public void Attached_DefaultInherits() => AssertGeneratedCode("Attached_DefaultInherits", """
        namespace TestNs;

        public partial class Grid : AvaloniaObject
        {
            [GeneratedAttachedProperty(DefaultValue = 1)]
            public static partial int GetRowSpan(Visual element);

            [GeneratedAttachedProperty(Inherits = true)]
            public static partial double GetFontSize(Visual element);
        }
        """);

    [Fact]
    public void Attached_ValidateCoerce() => AssertGeneratedCode("Attached_ValidateCoerce", """
        namespace TestNs;

        public partial class Grid : AvaloniaObject
        {
            [GeneratedAttachedProperty(ValidateMethodName = nameof(ValidateOrder), CoerceMethodName = nameof(CoerceOrder), DefaultValue = 0)]
            public static partial int GetOrder(Visual element);

            private static bool ValidateOrder(int value) => value >= 0;

            private static int CoerceOrder(AvaloniaObject sender, int value) => value < 0 ? 0 : value;
        }
        """);

    [Fact]
    public void Attached_NonPublicAccessors() => AssertGeneratedCode("Attached_NonPublicAccessors", """
        namespace TestNs;

        public partial class Host : AvaloniaObject
        {
            [GeneratedAttachedProperty]
            internal static partial bool GetIsHosted(Visual element);
        }
        """);

    [Fact]
    public void Attached_Nullable() => AssertGeneratedCode("Attached_Nullable", """
        namespace TestNs;

        public partial class ToolTip : AvaloniaObject
        {
            [GeneratedAttachedProperty]
            public static partial string? GetTip(Visual element);
        }
        """);

    [Fact]
    public void Attached_StaticOwner() => AssertGeneratedCode("Attached_StaticOwner", """
        namespace TestNs;

        public static partial class ScrollHelper
        {
            [GeneratedAttachedProperty(DefaultValue = false)]
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
            [GeneratedAttachedProperty(AddOwnerFrom = typeof(BasePanel), DefaultValue = 2)]
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
                [GeneratedStyledProperty]
                public partial string? Header { get; set; }
            }
        }
        """,
        expectedHintName: "TestNs.Outer.MyControl.AvaloniaProperties.g.cs");

    [Fact]
    public void GlobalNamespace() => AssertGeneratedCode("GlobalNamespace", """
        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty]
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
            [GeneratedStyledProperty]
            public partial T? Item { get; set; }
        }
        """,
        expectedHintName: "TestNs.MyControl_1.AvaloniaProperties.g.cs");

    [Fact]
    public void MultiProperty() => AssertGeneratedCode("MultiProperty", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty]
            public partial bool First { get; set; }

            [GeneratedStyledProperty]
            public partial bool Second { get; set; }

            [GeneratedDirectProperty]
            public partial string Text { get; set; } = "";

            [GeneratedAttachedProperty]
            public static partial int GetOrder(Visual element);
        }
        """);

    [Fact]
    public void NullableDisabled_Styled() => AssertGeneratedCode("NullableDisabled_Styled", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty]
            public partial string Header { get; set; }

            [GeneratedStyledProperty]
            public partial int Width { get; set; }
        }
        """,
        nullableContextOptions: NullableContextOptions.Disable);

    [Fact]
    public void NullableDisabled_Attached() => AssertGeneratedCode("NullableDisabled_Attached", """
        namespace TestNs;

        public partial class MyPanel : AvaloniaObject
        {
            [GeneratedAttachedProperty]
            public static partial string GetLabel(Visual element);
        }
        """,
        nullableContextOptions: NullableContextOptions.Disable);

    [Fact]
    public void NullableDisabled_ValidateCoerce() => AssertGeneratedCode("NullableDisabled_ValidateCoerce", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty(ValidateMethodName = nameof(ValidateHeader), CoerceMethodName = nameof(CoerceHeader))]
            public partial string Header { get; set; }

            private static bool ValidateHeader(string value) => true;

            private static string CoerceHeader(AvaloniaObject sender, string value) => value;
        }
        """,
        nullableContextOptions: NullableContextOptions.Disable);

    [Fact]
    public void NullableMixed_InlineDirective() => AssertGeneratedCode("NullableMixed_InlineDirective", """
        namespace TestNs;

        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty]
            public partial string? Enabled { get; set; }

        #nullable disable
            [GeneratedStyledProperty]
            public partial string Disabled { get; set; }

            [GeneratedAttachedProperty]
            public static partial string GetDisabledAttached(Visual element);
        #nullable restore

            [GeneratedDirectProperty]
            public partial string EnabledAgain { get; set; } = "";
        }
        """);
}
