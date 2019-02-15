using System.Linq;
using XamlIl.Transform;
using XamlIl.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    public class AvaloniaPropertyDescriptorEmitter
    {
        public static bool Emit(XamlIlEmitContext context, IXamlIlEmitter emitter, IXamlIlProperty property)
        {
            var type = (property.Getter ?? property.Setter).DeclaringType;
            var name = property.Name + "Property";
            var found = type.Fields.FirstOrDefault(f => f.IsStatic && f.Name == name);
            if (found == null)
                return false;

            emitter.Ldsfld(found);
            return true;
        }
    }
}
