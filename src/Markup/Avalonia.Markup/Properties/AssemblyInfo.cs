using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Metadata;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Data")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Markup.Data")]

[assembly: TypeForwardedTo(typeof(CultureInfoIetfLanguageTagConverter))]
