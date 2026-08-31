using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Analyzers.GeneratedProperties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Avalonia.Analyzers.Tests.GeneratedProperties;

public class ConvertToGeneratedPropertyAnalyzerTests
{
    private static Task Verify([StringSyntax("csharp")] string source, LanguageVersion? languageVersion = null)
    {
        var test = new ConvertToGeneratedPropertyAnalyzerTest
        {
            TestCode = """
                       using Avalonia;
                       using Avalonia.Data;

                       """ + source
        };
        if (languageVersion is { } version)
        {
            test.LanguageVersion = version;
        }

        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public Task Styled_Minimal_Reports_AVP2102() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<double> {|AVP2102:ScaleProperty|} =
                AvaloniaProperty.Register<MyControl, double>(nameof(Scale));

            public double Scale
            {
                get => GetValue(ScaleProperty);
                set => SetValue(ScaleProperty, value);
            }
        }
        """);

    [Fact]
    public Task Styled_Constant_Default_Reports_AVP2102() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<double> {|AVP2102:ScaleProperty|} =
                AvaloniaProperty.Register<MyControl, double>(nameof(Scale), 1.0d);

            public double Scale
            {
                get => GetValue(ScaleProperty);
                set => SetValue(ScaleProperty, value);
            }
        }
        """);

    [Fact]
    public Task Styled_All_Constant_Arguments_Report_AVP2102() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<string?> {|AVP2102:HeaderProperty|} =
                AvaloniaProperty.Register<MyControl, string?>(
                    nameof(Header), "header", inherits: true,
                    defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);

            public string? Header
            {
                get => GetValue(HeaderProperty);
                set => SetValue(HeaderProperty, value);
            }
        }
        """);

    [Fact]
    public Task Styled_Block_Bodies_Report_AVP2102() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<double> {|AVP2102:ScaleProperty|} =
                AvaloniaProperty.Register<MyControl, double>("Scale");

            public double Scale
            {
                get { return (double)this.GetValue(ScaleProperty); }
                set { this.SetValue(ScaleProperty, value); }
            }
        }
        """);

    [Fact]
    public Task Styled_Restricted_Setter_Reports_AVP2102() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<int> {|AVP2102:CountProperty|} =
                AvaloniaProperty.Register<MyControl, int>(nameof(Count));

            public int Count
            {
                get => GetValue(CountProperty);
                private set => SetValue(CountProperty, value);
            }
        }
        """);

    [Fact]
    public Task Direct_ReadWrite_Reports_AVP2102() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly DirectProperty<MyControl, string> {|AVP2102:TextProperty|} =
                AvaloniaProperty.RegisterDirect<MyControl, string>(
                    nameof(Text), o => o.Text, (o, v) => o.Text = v);

            private string _text = "";

            public string Text
            {
                get => _text;
                set => SetAndRaise(TextProperty, ref _text, value);
            }
        }
        """);

    [Fact]
    public Task Direct_ReadOnly_Reports_AVP2102() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly DirectProperty<MyControl, string> {|AVP2102:TextProperty|} =
                AvaloniaProperty.RegisterDirect<MyControl, string>(nameof(Text), static o => o.Text);

            private string _text = "";

            public string Text
            {
                get => _text;
                private set => SetAndRaise(TextProperty, ref _text, value);
            }
        }
        """);

    [Fact]
    public Task Attached_Reports_AVP2102() => Verify(
        """
        public class MyPanel : AvaloniaObject
        {
            public static readonly AttachedProperty<int> {|AVP2102:RowProperty|} =
                AvaloniaProperty.RegisterAttached<MyPanel, Visual, int>("Row");

            public static int GetRow(Visual element) => element.GetValue(RowProperty);

            public static void SetRow(Visual element, int value) => element.SetValue(RowProperty, value);
        }
        """);

    [Fact]
    public Task Attached_On_Static_Class_Reports_AVP2102() => Verify(
        """
        public static class Helper
        {
            public static readonly AttachedProperty<int> {|AVP2102:RowProperty|} =
                AvaloniaProperty.RegisterAttached<Visual, int>("Row", typeof(Helper));

            public static int GetRow(Visual element) => element.GetValue(RowProperty);

            public static void SetRow(Visual element, int value) => element.SetValue(RowProperty, value);
        }
        """);

    [Fact]
    public Task Styled_AddOwner_Reports_AVP2102() => Verify(
        """
        public class RangeBase : AvaloniaObject
        {
            // Own registration: also a candidate.
            public static readonly StyledProperty<double> {|AVP2102:ValueProperty|} =
                AvaloniaProperty.Register<RangeBase, double>("Value");
        }

        public class MyControl : RangeBase
        {
            public static readonly StyledProperty<double> {|AVP2102:ValueProperty|} =
                RangeBase.ValueProperty.AddOwner<MyControl>();

            public double Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }
        }
        """);

    [Fact]
    public Task Direct_AddOwner_Reports_AVP2102() => Verify(
        """
        public class TextBase : AvaloniaObject
        {
            // Own registration: also a candidate.
            public static readonly DirectProperty<TextBase, string> {|AVP2102:TextProperty|} =
                AvaloniaProperty.RegisterDirect<TextBase, string>("Text", o => "");
        }

        public class MyControl : TextBase
        {
            public static readonly DirectProperty<MyControl, string> {|AVP2102:TextProperty|} =
                TextBase.TextProperty.AddOwner<MyControl>(o => o.Text, (o, v) => o.Text = v);

            private string _text = "";

            public string Text
            {
                get => _text;
                set => SetAndRaise(TextProperty, ref _text, value);
            }
        }
        """);

    [Fact]
    public Task Validate_Callback_Reports_AVP2102() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<double> {|AVP2102:ScaleProperty|} =
                AvaloniaProperty.Register<MyControl, double>(nameof(Scale), validate: v => v >= 0);

            public double Scale
            {
                get => GetValue(ScaleProperty);
                set => SetValue(ScaleProperty, value);
            }
        }
        """);

    [Fact]
    public Task Coerce_Method_Group_Reports_AVP2102() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<double> {|AVP2102:ScaleProperty|} =
                AvaloniaProperty.Register<MyControl, double>(nameof(Scale), coerce: CoerceScale);

            private static double CoerceScale(AvaloniaObject sender, double value) => value;

            public double Scale
            {
                get => GetValue(ScaleProperty);
                set => SetValue(ScaleProperty, value);
            }
        }
        """);

    [Fact]
    public Task NonConstant_Default_Reports_AVP2102() => Verify(
        """
        using Avalonia.Media;

        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<Color> {|AVP2102:BarColorProperty|} =
                AvaloniaProperty.Register<MyControl, Color>(nameof(BarColor), Colors.White);

            public Color BarColor
            {
                get => GetValue(BarColorProperty);
                set => SetValue(BarColorProperty, value);
            }
        }
        """);

    [Fact]
    public Task Side_Effecting_Accessor_Reports_AVP2102() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<double> {|AVP2102:ScaleProperty|} =
                AvaloniaProperty.Register<MyControl, double>(nameof(Scale));

            private int _setCount;

            public double Scale
            {
                get => GetValue(ScaleProperty);
                set
                {
                    _setCount++;
                    SetValue(ScaleProperty, value);
                }
            }
        }
        """);

    [Fact]
    public Task Owner_Mismatch_Reports_Nothing() => Verify(
        """
        public class OtherControl : AvaloniaObject
        {
        }

        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<double> ScaleProperty =
                AvaloniaProperty.Register<OtherControl, double>("Scale");

            public double Scale
            {
                get => GetValue(ScaleProperty);
                set => SetValue(ScaleProperty, value);
            }
        }
        """);

    [Fact]
    public Task Field_Name_Without_Property_Suffix_Reports_Nothing() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<double> ScaleProp =
                AvaloniaProperty.Register<MyControl, double>("Scale");

            public double Scale
            {
                get => GetValue(ScaleProp);
                set => SetValue(ScaleProp, value);
            }
        }
        """);

    [Fact]
    public Task Generic_Containing_Type_Reports_Nothing() => Verify(
        """
        public class MyControl<T> : AvaloniaObject
        {
            public static readonly StyledProperty<double> ScaleProperty =
                AvaloniaProperty.Register<MyControl<T>, double>(nameof(Scale));

            public double Scale
            {
                get => GetValue(ScaleProperty);
                set => SetValue(ScaleProperty, value);
            }
        }
        """);

    [Fact]
    public Task Multiple_Declarators_Report_Nothing() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<double>
                ScaleProperty = AvaloniaProperty.Register<MyControl, double>(nameof(Scale)),
                ZoomProperty = AvaloniaProperty.Register<MyControl, double>(nameof(Zoom));

            public double Scale
            {
                get => GetValue(ScaleProperty);
                set => SetValue(ScaleProperty, value);
            }

            public double Zoom
            {
                get => GetValue(ZoomProperty);
                set => SetValue(ZoomProperty, value);
            }
        }
        """);

    [Fact]
    public Task Field_Alias_Reports_Nothing() => Verify(
        """
        using Avalonia.Layout;

        public class MyControl : AvaloniaObject
        {
            // Plain alias of an existing property, not a Register/AddOwner call.
            public static readonly StyledProperty<double> WidthProperty = Layoutable.WidthProperty;
        }
        """);

    [Fact]
    public Task CSharp12_Reports_Nothing() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<double> ScaleProperty =
                AvaloniaProperty.Register<MyControl, double>(nameof(Scale));

            public double Scale
            {
                get => GetValue(ScaleProperty);
                set => SetValue(ScaleProperty, value);
            }
        }
        """,
        LanguageVersion.CSharp12);

    [Fact]
    public Task CSharp13_Reports_AVP2102() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            public static readonly StyledProperty<double> {|AVP2102:ScaleProperty|} =
                AvaloniaProperty.Register<MyControl, double>(nameof(Scale));

            public double Scale
            {
                get => GetValue(ScaleProperty);
                set => SetValue(ScaleProperty, value);
            }
        }
        """,
        LanguageVersion.CSharp13);

    public class ConvertToGeneratedPropertyAnalyzerTest : CSharpAnalyzerTest<ConvertToGeneratedPropertyAnalyzer, DefaultVerifier>
    {
        public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp14;

        public ConvertToGeneratedPropertyAnalyzerTest()
        {
            ReferenceAssemblies = new ReferenceAssemblies(TestReferences.DefaultTargetFramework);
            foreach (var reference in TestReferences.All.Value)
            {
                TestState.AdditionalReferences.Add(reference);
            }

            // The manual declarations under test are valid code, but keep parity with the
            // other generated-property tests and only assert our own diagnostics.
            CompilerDiagnostics = CompilerDiagnostics.None;
        }

        protected override ParseOptions CreateParseOptions()
            => ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
    }
}
