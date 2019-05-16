// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Metadata;

[assembly: InternalsVisibleTo("Avalonia.Controls.UnitTests")]
[assembly: InternalsVisibleTo("Avalonia.DesignerSupport")]

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls.Collections")]
[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Avalonia.Controls.Primitives")]
