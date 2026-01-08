using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Utilities;
using Xunit;

namespace Avalonia.Build.Tasks.UnitTest;

public class CompileAvaloniaXamlTaskTest
{

    [Fact]
    public void Does_Not_Fail_When_Codebehind_Contains_DllImport()
    {
        using var engine = UnitTestBuildEngine.Start();
        var basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Assets");
        var assembly = new TaskItem(Path.Combine(basePath, "PInvoke.dll"));
        assembly.SetMetadata(CompileAvaloniaXamlTask.AvaloniaCompileOutputMetadataName, Path.Combine(basePath, "Avalonia", Path.GetFileName(assembly.ItemSpec)));
        var references = File.ReadAllLines(Path.Combine(basePath, "PInvoke.dll.refs")).Select(p => new TaskItem(p)).ToArray();

        Assert.True(File.Exists(assembly.ItemSpec), $"The original {assembly.ItemSpec} don't exist.");

        new CompileAvaloniaXamlTask()
        {
            AssemblyFile = assembly,
            References = references,
            RefAssemblyFile = null,
            BuildEngine = engine,
            ProjectDirectory = Directory.GetCurrentDirectory(),
            VerifyIl = true
        }.Execute();
        Assert.Empty(engine.Errors);
    }


}
