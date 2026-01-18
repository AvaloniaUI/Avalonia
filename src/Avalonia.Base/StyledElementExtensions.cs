using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;

namespace Avalonia
{
    public static class StyledElementExtensions
    {
        public static IDisposable BindClasses(this StyledElement target, BindingBase source, object? anchor = null) =>
            ClassBindingManager.BindClasses(target, source, anchor);

        public static void SetClasses(this StyledElement target, string classNames) =>
            ClassBindingManager.SetClasses(target, classNames);

        public static IDisposable BindClass(this StyledElement target, string className, BindingBase source, object? anchor = null) =>
            ClassBindingManager.BindClass(target, className, source, anchor);

        public static void SetClass(this StyledElement target, string className, bool value) =>
            ClassBindingManager.SetClass(target, className, value);

        public static AvaloniaProperty GetClassProperty(string className) =>
            ClassBindingManager.GetClassProperty(className);

        internal static bool IsClassesBindingProperty(this AvaloniaProperty property, [NotNullWhen(true)] out string? classPropertyName) =>
            ClassBindingManager.IsClassesBindingProperty(property, out classPropertyName);
    }
}
