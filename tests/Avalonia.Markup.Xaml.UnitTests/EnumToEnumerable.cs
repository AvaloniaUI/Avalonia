using System;

namespace Avalonia.Markup.Xaml.UnitTests
{
    public class EnumToEnumerable : MarkupExtension
    {
        private readonly Type _enumType;

        public EnumToEnumerable(Type type)
        {
            if (!type.IsEnum)
                throw new ArgumentException(nameof(type));
            _enumType = type;
        }
        
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Enum.GetValues(_enumType);
        }
    }
}
