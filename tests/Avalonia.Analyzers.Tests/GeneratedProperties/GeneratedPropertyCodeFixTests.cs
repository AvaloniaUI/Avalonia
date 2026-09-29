using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Analyzers.CodeFixes.CSharp;
using Avalonia.Analyzers.GeneratedProperties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Avalonia.Analyzers.Tests.GeneratedProperties;

public class GeneratedPropertyCodeFixTests
{
    private static Task FixAndVerify<TCodeFix>(
        [StringSyntax("csharp")] string testCode,
        [StringSyntax("csharp")] string fixedCode,
        int? fixAllIterations = null)
        where TCodeFix : CodeFixProvider, new()
    {
        var test = new GeneratedPropertyCodeFixTest<TCodeFix>
        {
            TestCode = """
                       using Avalonia;
                       using Avalonia.Data;

                       """ + testCode,
            FixedCode = """
                        using Avalonia;
                        using Avalonia.Data;

                        """ + fixedCode,
            NumberOfFixAllIterations = fixAllIterations
        };

        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public Task MakePartial_Adds_Partial_To_Member() => FixAndVerify<MakePartialCodeFixProvider>(
        """
        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty]
            public string? {|AVP2007:Header|} { get; set; }
        }
        """,
        """
        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty]
            public partial string? Header { get; set; }
        }
        """);

    [Fact]
    public Task MakePartial_Adds_Partial_To_Containing_Types() => FixAndVerify<MakePartialCodeFixProvider>(
        """
        public class Outer
        {
            public class MyControl : AvaloniaObject
            {
                [GeneratedStyledProperty]
                public partial string? {|AVP2007:Header|} { get; set; }
            }
        }
        """,
        """
        public partial class Outer
        {
            public partial class MyControl : AvaloniaObject
            {
                [GeneratedStyledProperty]
                public partial string? Header { get; set; }
            }
        }
        """);

    [Fact]
    public Task AddCallbackStub_Inserts_Validate_And_Coerce() => FixAndVerify<AddCallbackStubCodeFixProvider>(
        """
        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty({|AVP2006:ValidateMethodName = "ValidateValue"|}, {|AVP2006:CoerceMethodName = "CoerceValue"|})]
            public partial int Value { get; set; }
        }
        """,
        """
        public partial class MyControl : AvaloniaObject
        {
            [GeneratedStyledProperty(ValidateMethodName = "ValidateValue", CoerceMethodName = "CoerceValue")]
            public partial int Value { get; set; }

            private static int CoerceValue(AvaloniaObject sender, int value)
            {
            }

            private static bool ValidateValue(int value)
            {
            }
        }
        """,
        fixAllIterations: 2);

    public class GeneratedPropertyCodeFixTest<TCodeFix> : CSharpCodeFixTest<GeneratedPropertyAnalyzer, TCodeFix, DefaultVerifier>
        where TCodeFix : CodeFixProvider, new()
    {
        public GeneratedPropertyCodeFixTest()
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
            => ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion.CSharp14);
    }
}
