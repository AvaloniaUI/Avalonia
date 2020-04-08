using Avalonia.Metadata;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Animation")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Animation.Easings")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Animation.Animators")]

[assembly: InternalsVisibleTo("Avalonia.LeakTests")]
[assembly: InternalsVisibleTo("Avalonia.Animation.UnitTests")]
