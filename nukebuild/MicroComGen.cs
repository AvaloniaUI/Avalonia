using System.IO;
using MicroCom.CodeGenerator;
using Nuke.Common;

partial class Build : NukeBuild
{
    Target GenerateCppHeaders => _ => _.Executes(() =>
    {
        var file = MicroComCodeGenerator.Parse(
            File.ReadAllText(RootDirectory / "src" / "Avalonia.Native" / "avn.idl"));
        File.WriteAllText(RootDirectory / "native" / "Avalonia.Native" / "inc" / "avalonia-native.h",
            file.GenerateCppHeader());
    });
}