// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reflection;
using Avalonia.Metadata;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("Avalonia.Markup.Xaml")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Markup.Xaml.Data")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Markup.Xaml.MarkupExtensions")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Markup.Xaml.Styling")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Markup.Xaml.Templates")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml", "Avalonia.Markup.Xaml.MarkupExtensions.Standard")]
[assembly: InternalsVisibleTo("Avalonia.Markup.Xaml.UnitTests")]

