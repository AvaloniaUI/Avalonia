using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace Avalonia.Build.Tasks.UnitTest;

public class CompileAvaloniaXamlTaskTest
{

    [Fact]
    public void Does_Not_Fail_When_Codebehind_Contains_DllImport()
    {
        using var engine = UnitTestBuildEngine.Start();
        var basePath = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "Assets");
        var originalAssemblyPath = Path.Combine(basePath,
            "PInvoke.dll");
        var referencesPath = Path.Combine(basePath,
                "PInvoke.dll.refs");
        var compiledAssemblyPath = "PInvoke.dll";

        Assert.True(File.Exists(originalAssemblyPath), $"The original {originalAssemblyPath} don't exists.");

        new CompileAvaloniaXamlTask()
        {
            AssemblyFile = originalAssemblyPath,
            ReferencesFilePath = referencesPath,
            OutputPath = compiledAssemblyPath,
            RefAssemblyFile = null,
            BuildEngine = engine,
            ProjectDirectory = Directory.GetCurrentDirectory(),
            VerifyIl = true
        }.Execute();
        Assert.Equal(0, engine.Errors.Count);
    }


}
