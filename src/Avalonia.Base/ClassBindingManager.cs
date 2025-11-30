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

        public static readonly AttachedProperty<HashSet<string>?> BoundClassesProperty =
            AvaloniaProperty.RegisterAttached<StyledElement, HashSet<string>?>(
                "BoundClasses", typeof(ClassBindingManager));

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

        public static void SetBoundClasses(StyledElement element, HashSet<string>? value)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            element.SetValue(BoundClassesProperty, value);
        }

        public static HashSet<string>? GetBoundClasses(StyledElement element)
        {
            _ = element ?? throw new ArgumentNullException(nameof(element));
            return element.GetValue(BoundClassesProperty);
        }

        static ClassBindingManager()
        {
            ClassesProperty.Changed.AddClassHandler<StyledElement, string>(ClassesPropertyChanged);
        }

        private static void ClassesPropertyChanged(StyledElement sender, AvaloniaPropertyChangedEventArgs<string> e)
        {
            var boundClasses = GetBoundClasses(sender);

            var newValue = e.GetNewValue<string?>() ?? "";
            var newValues = newValue.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var currentValues = sender.Classes
                .Where(c => !c.StartsWith(":", StringComparison.Ordinal) && boundClasses?.Contains(c) != true)
                .ToList();
            if (currentValues.SequenceEqual(newValues))
                return;

            sender.Classes.Replace(currentValues, newValues);
        }

        private static void AddBoundClass(StyledElement target, string className)
        {
            var boundClasses = GetBoundClasses(target);
            if (boundClasses == null)
            {
                boundClasses = [];
                SetBoundClasses(target, boundClasses);
            }
            boundClasses.Add(className);
        }

        public static IDisposable BindClasses(StyledElement target, BindingBase source, object anchor)
        {
            return target.Bind(ClassesProperty, source);
        }

        public static void SetClass(StyledElement target, string className, bool value)
        {
            AddBoundClass(target, className);
            target.Classes.Set(className, value);
        }

        public static IDisposable BindClass(StyledElement target, string className, BindingBase source, object anchor)
        {
            AddBoundClass(target, className);
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
                var sender = (StyledElement)args.Sender;
                SetClass(sender, className, args.NewValue.GetValueOrDefault());
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
