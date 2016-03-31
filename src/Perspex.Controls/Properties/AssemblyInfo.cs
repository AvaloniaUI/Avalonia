// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Perspex.Metadata;
using Portable.Xaml.Markup;

[assembly: AssemblyTitle("Perspex.Controls")]
[assembly: InternalsVisibleTo("Perspex.Controls.UnitTests")]
[assembly: InternalsVisibleTo("Perspex.DesignerSupport")]

[assembly: XmlnsDefinition("https://github.com/perspex", "Perspex")]
[assembly: XmlnsDefinition("https://github.com/perspex", "Perspex.Controls")]
[assembly: XmlnsDefinition("https://github.com/perspex", "Perspex.Controls.Presenters")]
[assembly: XmlnsDefinition("https://github.com/perspex", "Perspex.Controls.Primitives")]
[assembly: XmlnsDefinition("https://github.com/perspex", "Perspex.Controls.Shapes")]
[assembly: XmlnsDefinition("https://github.com/perspex", "Perspex.Controls.Templates")]