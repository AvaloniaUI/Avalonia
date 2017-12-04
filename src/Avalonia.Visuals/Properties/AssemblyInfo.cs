// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Metadata;

[assembly: AssemblyTitle("Avalonia.Visuals")]
[assembly: InternalsVisibleTo("Avalonia.Visuals.UnitTests")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Animation")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Media")]

[assembly: InternalsVisibleTo("Avalonia.Direct2D1.RenderTests")]
[assembly: InternalsVisibleTo("Avalonia.Skia.RenderTests")]