using System;
using System.Linq;
using Mono.Cecil;
using Nuke.Common.Tooling;
using Serilog;

internal static class XamlCompilationVerifier
{
    public static void VerifyAssemblyCompiledXaml(string assemblyPath)
    {
        const string avaloniaResourcesTypeName = "CompiledAvaloniaXaml.!AvaloniaResources";
        const string mainViewTypeName = "BuildTests.MainView";
        const string populateMethodName = "!XamlIlPopulate";

        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

        if (assembly.MainModule.GetType(avaloniaResourcesTypeName) is null)
        {
            throw new InvalidOperationException(
                $"Assembly {assemblyPath} is missing type {avaloniaResourcesTypeName}");
        }

        if (assembly.MainModule.GetType(mainViewTypeName) is not { } mainViewType)
        {
            throw new InvalidOperationException(
                $"Assembly {assemblyPath} is missing type {mainViewTypeName}");
        }

        if (!mainViewType.Methods.Any(method => method.Name == populateMethodName))
        {
            throw new InvalidOperationException(
                $"Assembly {assemblyPath} is missing method {populateMethodName} on {mainViewTypeName}");
        }

        Log.Information($"Assembly {assemblyPath} correctly has compiled XAML");
    }

    public static void VerifyNativeAot(string programPath)
    {
        const string expectedOutput = "Hello from AOT";

        using var process = ProcessTasks.StartProcess(programPath, string.Empty);

        process.WaitForExit();
        process.AssertZeroExitCode();

        var output = process.Output.Select(o => o.Text).FirstOrDefault();
        if (output != expectedOutput)
        {
            throw new InvalidOperationException(
                $"{programPath} returned text \"{output}\", expected \"{expectedOutput}\"");
        }

        Log.Information($"Native program {programPath} correctly has compiled XAML");
    }
}
