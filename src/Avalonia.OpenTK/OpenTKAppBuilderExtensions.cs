using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenTK;

namespace Avalonia
{
    public static class AvaloniaOpenTKAppBuilderExtensions
    {
        public static T UseOpenTK<T>(
            this T builder, IList<GlVersion> probeVersions)
            where T : AppBuilderBase<T>, new()
        {
            return builder.AfterPlatformServicesSetup(_ => AvaloniaOpenTKIntegration.Initialize(probeVersions));
        }
    }
}
