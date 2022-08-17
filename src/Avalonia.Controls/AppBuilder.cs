using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Avalonia
{
    /// <summary>
    /// Initializes platform-specific services for an <see cref="Application"/>.
    /// </summary>
    public sealed class AppBuilder : AppBuilderBase<AppBuilder>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppBuilder"/> class.
        /// </summary>
        public AppBuilder()
            : base(new StandardRuntimePlatform(),
                builder => StandardRuntimePlatformServices.Register(GetAssemblyOrNull(builder.ApplicationType)))
        {
        }

        static Assembly? GetAssemblyOrNull(Type? t)
        {
            try
            {
                return t?.Assembly;
            }
            catch
            {
                return null;
            }
        }
    }
}
