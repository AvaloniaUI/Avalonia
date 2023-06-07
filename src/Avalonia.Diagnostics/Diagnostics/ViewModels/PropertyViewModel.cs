using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class PropertyViewModel : ViewModelBase
    {
        public abstract object Key { get; }
        public abstract string Name { get; }
        public abstract string Group { get; }
        public abstract Type AssignedType { get; }
        public abstract Type? DeclaringType { get; }
        public abstract object? Value { get; set; }
        public abstract string Priority { get; }
        public abstract bool? IsAttached { get; }
        public abstract void Update();
        public abstract Type PropertyType { get; }

        public string Type => PropertyType == AssignedType ?
            PropertyType.GetTypeName() :
            $"{PropertyType.GetTypeName()} {{{AssignedType.GetTypeName()}}}";

        public abstract bool IsReadonly { get; }
    }
}
