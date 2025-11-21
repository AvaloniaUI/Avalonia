using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;

namespace Avalonia
{
    public static class StyledElementExtensions
    {
        public static IDisposable BindClass(this StyledElement target, string className, BindingBase source, object anchor) =>
            ClassBindingManager.Bind(target, className, source, anchor);

        public static AvaloniaProperty GetClassProperty(string className) =>
            ClassBindingManager.GetClassProperty(className);

        internal static bool IsClassesBindingProperty(this AvaloniaProperty property, [NotNullWhen(true)] out string? classPropertyName) =>
            ClassBindingManager.IsClassesBindingProperty(property, out classPropertyName);
    }
}
