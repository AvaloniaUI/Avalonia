using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Metadata;

[assembly: InternalsVisibleTo("Avalonia.Visuals.UnitTests")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Animation")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Media")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Media.Imaging")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Media.Transformation")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia")]

[assembly: InternalsVisibleTo("Avalonia.Direct2D1.RenderTests")]
[assembly: InternalsVisibleTo("Avalonia.Skia.RenderTests")]
[assembly: InternalsVisibleTo("Avalonia.Skia.UnitTests")]
