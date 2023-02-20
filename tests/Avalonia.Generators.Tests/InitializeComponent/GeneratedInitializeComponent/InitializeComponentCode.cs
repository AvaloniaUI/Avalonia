using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Avalonia.Generators.Tests.InitializeComponent.GeneratedInitializeComponent;

public static class InitializeComponentCode
{
    public const string NamedControl = "NamedControl.txt";
    public const string NamedControls = "NamedControls.txt";
    public const string XNamedControl = "xNamedControl.txt";
    public const string XNamedControls = "xNamedControls.txt";
    public const string NoNamedControls = "NoNamedControls.txt";
    public const string CustomControls = "CustomControls.txt";
    public const string DataTemplates = "DataTemplates.txt";
    public const string SignUpView = "SignUpView.txt";
    public const string FieldModifier = "FieldModifier.txt";
    public const string AttachedProps = "AttachedProps.txt";
    public const string AttachedPropsWithDevTools = "AttachedPropsWithDevTools.txt";
    public const string ControlWithoutWindow = "ControlWithoutWindow.txt";
        
    public static async Task<string> Load(string generatedCodeResourceName)
    {
        var assembly = typeof(XamlXNameResolverTests).Assembly;
        var fullResourceName = assembly
            .GetManifestResourceNames()
            .First(name => name.Contains("InitializeComponent") &&
                           name.Contains("GeneratedInitializeComponent") &&
                           name.EndsWith(generatedCodeResourceName));

        await using var stream = assembly.GetManifestResourceStream(fullResourceName);
        using var reader = new StreamReader(stream!);
        return await reader.ReadToEndAsync();
    }
}