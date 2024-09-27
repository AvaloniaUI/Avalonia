using System;
using Avalonia.Platform;

namespace IntegrationTestApp.Embedding;

internal interface INativeControlFactory
{
    IPlatformHandle CreateControl(IPlatformHandle parent, Func<IPlatformHandle> createDefault);
}
