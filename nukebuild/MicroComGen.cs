using System.IO;
using MicroComGenerator;
using Nuke.Common;

partial class Build : NukeBuild
{
    Target GenerateCppHeaders => _ => _.Executes(() =>
    {
        var text = File.ReadAllText(RootDirectory / "src" / "Avalonia.Native" / "avn.idl");
        var ast = AstParser.Parse(text);
        File.WriteAllText(RootDirectory / "native" / "Avalonia.Native" / "inc" / "avalonia-native.h",
            CppGen.GenerateCpp(ast));
    });
}