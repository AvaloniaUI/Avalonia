using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Analyzers.GeneratedProperties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Avalonia.Analyzers.Tests.GeneratedProperties;

public class GeneratedPropertyAnalyzerTests
{
    private static Task Verify([StringSyntax("csharp")] string source, LanguageVersion? languageVersion = null)
    {
        var test = new GeneratedPropertyAnalyzerTest
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
    public Task Valid_Properties_Report_Nothing() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty(DefaultValue = 100)]
            public partial int Width { get; set; }

            [DirectProperty]
            public partial string Text { get; set; } = "";

            [AttachedProperty]
            public static partial int GetRow(Visual element);
        }
        """);

    [Fact]
    public Task Valid_Callback_Reports_Nothing() => Verify(
        """
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
    public Task Missing_Base_Class_Reports_AVP2001() => Verify(
        """
        public partial class MyControl
        {
            [StyledProperty]
            public partial string? {|AVP2001:Header|} { get; set; }
        }
        """);

    [Fact]
    public Task Attached_AddOwner_On_Static_Class_Reports_AVP2001() => Verify(
        """
        public class BasePanel : AvaloniaObject
        {
            public static readonly AttachedProperty<int> RowProperty =
                AvaloniaProperty.RegisterAttached<BasePanel, Visual, int>("Row");
        }

        public static partial class Helper
        {
            // AddOwner<T> requires generic host parameter, which is not possible in this context (static class) 
            [AttachedProperty(AddOwnerFrom = typeof(BasePanel))]
            public static partial int {|AVP2001:GetRow|}(Visual element);
        }
        """);

    [Fact]
    public Task Attached_On_Static_Class_Reports_Nothing() => Verify(
        """
        public static partial class Helper
        {
            [AttachedProperty]
            public static partial int GetRow(Visual element);
        }
        """);

    [Fact]
    public Task Inherits_With_Add_Owner_Reports_AVP2002() => Verify(
        """
        public class RangeBase : AvaloniaObject
        {
            public static readonly StyledProperty<double> ValueProperty =
                AvaloniaProperty.Register<RangeBase, double>("Value");
        }

        public partial class MyControl : RangeBase
        {
            // AddOwner() doesn't have inherits argument.
            [StyledProperty(AddOwnerFrom = typeof(RangeBase), {|AVP2002:Inherits = true|})]
            public partial double Value { get; set; }
        }
        """);

    [Fact]
    public Task Multiple_Generator_Attributes_Report_AVP2002() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty]
            [DirectProperty]
            public partial string? {|AVP2002:Header|} { get; set; }
        }
        """);

    [Fact]
    public Task Incompatible_Default_Value_Reports_AVP2003() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty({|AVP2003:DefaultValue = "text"|})]
            public partial int Value { get; set; }
        }
        """);

    [Fact]
    public Task Incompatible_Unset_Value_Reports_AVP2003() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            [DirectProperty({|AVP2003:UnsetValue = "text"|})]
            public partial int Value { get; set; }
        }
        """);

    [Fact]
    public Task Missing_Add_Owner_Source_Reports_AVP2004() => Verify(
        """
        public class EmptyBase : AvaloniaObject
        {
            // ValueProperty expected
        }

        public partial class MyControl : EmptyBase
        {
            [StyledProperty({|AVP2004:AddOwnerFrom = typeof(EmptyBase)|})]
            public partial double Value { get; set; }
        }
        """);

    [Fact]
    public Task AddOwner_From_Generated_Styled_Property_Reports_Nothing() => Verify(
        """
        public partial class RangeBase : AvaloniaObject
        {
            // Source property itself is source-generated.
            [StyledProperty]
            public partial double Value { get; set; }
        }

        public partial class MyControl : RangeBase
        {
            [StyledProperty(AddOwnerFrom = typeof(RangeBase))]
            public new partial double Value { get; set; }
        }
        """);

    [Fact]
    public Task AddOwner_From_Generated_Direct_Property_Reports_Nothing() => Verify(
        """
        public partial class TextBase : AvaloniaObject
        {
            [DirectProperty]
            public partial string? Text { get; set; }
        }

        public partial class MyControl : TextBase
        {
            [DirectProperty(AddOwnerFrom = typeof(TextBase))]
            public new partial string? Text { get; set; }
        }
        """);

    [Fact]
    public Task AddOwner_From_Generated_Attached_Property_Reports_Nothing() => Verify(
        """
        public partial class BasePanel : AvaloniaObject
        {
            [AttachedProperty]
            public static partial int GetRow(Visual element);
        }

        public partial class MyPanel : BasePanel
        {
            [AttachedProperty(AddOwnerFrom = typeof(BasePanel))]
            public static partial int GetRow(Visual element);
        }
        """);

    [Fact]
    public Task AddOwner_From_Generated_Property_Type_Mismatch_Reports_AVP2004() => Verify(
        """
        public partial class RangeBase : AvaloniaObject
        {
            [StyledProperty]
            public partial double Value { get; set; }
        }

        public partial class MyControl : RangeBase
        {
            // Source generated property exists but its value type is 'double', not 'int'.
            [StyledProperty({|AVP2004:AddOwnerFrom = typeof(RangeBase)|})]
            public new partial int Value { get; set; }
        }
        """);

    [Fact]
    public Task Attached_Without_Get_Prefix_Reports_AVP2005() => Verify(
        """
        public partial class Grid : AvaloniaObject
        {
            [AttachedProperty]
            public static partial int {|AVP2005:Row|}(Visual element);
        }
        """);

    [Fact]
    public Task Attached_Not_Static_Reports_AVP2005() => Verify(
        """
        public partial class Grid : AvaloniaObject
        {
            // GetRow expected to be static
            [AttachedProperty]
            public partial int {|AVP2005:GetRow|}(Visual element);
        }
        """);

    [Fact]
    public Task Missing_Callback_Reports_AVP2006() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty({|AVP2006:ChangedMethodName = "OnIsOpenChanged"|})]
            public partial bool IsOpen { get; set; }
        }
        """);

    [Fact]
    public Task Wrong_Signature_Callback_Reports_AVP2006() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty({|AVP2006:ChangedMethodName = nameof(OnIsOpenChanged)|})]
            public partial bool IsOpen { get; set; }

            // 'int' instead of 'bool'
            private partial void OnIsOpenChanged(int oldValue, int newValue)
            {
            }
        }
        """);

    [Fact]
    public Task Unimplemented_Partial_Callback_reports_AVP2006() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty({|AVP2006:ChangedMethodName = nameof(OnIsOpenChanged)|})]
            public partial bool IsOpen { get; set; }

            private partial void OnIsOpenChanged(bool oldValue, bool newValue);
        }
        """);

    [Fact]
    public Task Invalid_Callback_Name_Reports_AVP2006() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty({|AVP2006:ChangedMethodName = "not a name"|})]
            public partial bool IsOpen { get; set; }
        }
        """);

    [Fact]
    public Task Non_Partial_Member_Reports_AVP2007() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty]
            public string? {|AVP2007:Header|} { get; set; }
        }
        """);

    [Fact]
    public Task Non_Partial_Containing_Type_Reports_AVP2007() => Verify(
        """
        public class MyControl : AvaloniaObject
        {
            [StyledProperty]
            public partial string? {|AVP2007:Header|} { get; set; }
        }
        """);

    [Fact]
    public Task Language_Below_13_Reports_AVP2007() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            // Partial properties require C# 13; below that the generator can't emit.
            [StyledProperty]
            public partial string? {|AVP2007:Header|} { get; set; }
        }
        """,
        LanguageVersion.CSharp12);

    [Fact]
    public Task CSharp13_Valid_Property_Reports_Nothing() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty]
            public partial string? Header { get; set; }
        }
        """,
        LanguageVersion.CSharp13);

    [Fact]
    public Task Static_Property_Reports_AVP2008() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            [StyledProperty]
            public static partial string? {|AVP2008:Header|} { get; set; }
        }
        """);

    [Fact]
    public Task Getter_Only_Property_Reports_AVP2008() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            // Styled properties are never truly readonly.
            [StyledProperty]
            public partial string? {|AVP2008:Header|} { get; }
        }
        """);

    [Fact]
    public Task Styled_Non_Public_Setter_Reports_AVP2101() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            // Styled properties are never truly readonly.
            [StyledProperty]
            public partial bool {|AVP2101:IsPressed|} { get; private set; }
        }
        """);

    [Fact]
    public Task Direct_Non_Public_Setter_Reports_Nothing() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            // Direct properties can actually be read-only.
            [DirectProperty]
            public partial int SelectedIndex { get; private set; } = -1;
        }
        """);

    [Fact]
    public Task Doubled_Suffix_Reports_AVP2100() => Verify(
        """
        public partial class MyControl : AvaloniaObject
        {
            // This code will generate HeaderPropertyProperty definition.
            // if user really needs it, they can suppress the warning.
            [StyledProperty]
            public partial string? {|AVP2100:HeaderProperty|} { get; set; }
        }
        """);

    [Fact]
    public Task Doubled_Suffix_On_Attached_Reports_AVP2100() => Verify(
        """
        public partial class Grid : AvaloniaObject
        {
            [AttachedProperty]
            public static partial int {|AVP2100:GetRowProperty|}(Visual element);
        }
        """);

    public class GeneratedPropertyAnalyzerTest : CSharpAnalyzerTest<GeneratedPropertyAnalyzer, DefaultVerifier>
    {
        public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.CSharp14;

        public GeneratedPropertyAnalyzerTest()
        {
            ReferenceAssemblies = new ReferenceAssemblies(TestReferences.DefaultTargetFramework);
            foreach (var reference in TestReferences.All.Value)
            {
                TestState.AdditionalReferences.Add(reference);
            }

            // Disable compiled diagnostics - it will fail anyway, because of missing partial implementations.
            // We only test out analyzers.
            CompilerDiagnostics = CompilerDiagnostics.None;
        }

        protected override ParseOptions CreateParseOptions()
            => ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
    }
}
