// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Platform;
using Avalonia.Direct2D1;

[assembly: ExportRenderingSubsystem(OperatingSystemType.WinNT, 1, "Direct2D1", typeof(Direct2D1Platform), nameof(Direct2D1Platform.Initialize),
    typeof(Direct2DChecker))]

[assembly: InternalsVisibleTo("Avalonia.Direct2D1.RenderTests")]
[assembly: InternalsVisibleTo("Avalonia.Direct2D1.UnitTests")]

