using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Data;
using Avalonia.Reactive;

namespace Avalonia
{
    internal static class ClassBindingManager
    {
        private const string ClassPropertyPrefix = "__AvaloniaReserved::Classes::";
        private static readonly Dictionary<string, AvaloniaProperty> s_RegisteredProperties =
            new Dictionary<string, AvaloniaProperty>();

        public static readonly AttachedProperty<string> ClassesProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, string>(
                "Classes", typeof(ClassBindingManager), "");

        public static void SetClasses(StyledElement element, string value)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            element.SetValue(ClassesProperty, value);
        }

        public static string GetClasses(StyledElement element)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            return element.GetValue(ClassesProperty);
        }

        static ClassBindingManager()
        {
            ClassesProperty.Changed.AddClassHandler<StyledElement, string>(ClassesPropertyChanged);
        }

        private static void ClassesPropertyChanged(StyledElement sender, AvaloniaPropertyChangedEventArgs<string> e)
        {
            var newValue = e.GetNewValue<string?>() ?? "";
            var newValues = newValue.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var currentValues = sender.Classes.Where(c => !c.StartsWith(":", StringComparison.Ordinal));
            if (currentValues.SequenceEqual(newValues))
                return;

            sender.Classes.Replace(newValues);
        }

        public static IDisposable BindClasses(StyledElement target, BindingBase source, object anchor)
        {
            return target.Bind(ClassesProperty, source);
        }

        public static IDisposable BindClass(StyledElement target, string className, BindingBase source, object anchor)
        {
            var prop = GetClassProperty(className);
            return target.Bind(prop, source);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1001:The same AvaloniaProperty should not be registered twice",
            Justification = "Classes.attr binding feature is implemented using intermediate avalonia properties for each class")]
        private static AvaloniaProperty RegisterClassProxyProperty(string className)
        {
            var prop = AvaloniaProperty.Register<StyledElement, bool>(ClassPropertyPrefix + className);
            prop.Changed.Subscribe(args =>
            {
                var classes = ((StyledElement)args.Sender).Classes;
                classes.Set(className, args.NewValue.GetValueOrDefault());
            });

            return prop;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static AvaloniaProperty GetClassProperty(string className)
        {
            var prefixedClassName = ClassPropertyPrefix + className;
            return s_RegisteredProperties.TryGetValue(prefixedClassName, out var property)
                ? property
                : s_RegisteredProperties[prefixedClassName] = RegisterClassProxyProperty(className);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool IsClassesBindingProperty(AvaloniaProperty property, [NotNullWhen(true)] out string? classPropertyName)
        {
            
            classPropertyName = default;
            if(property.Name?.StartsWith(ClassPropertyPrefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                classPropertyName = property.Name.Substring(ClassPropertyPrefix.Length + 1);
                return true;
            }
            return false;
        }
    }
}
