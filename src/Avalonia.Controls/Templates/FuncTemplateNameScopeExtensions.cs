using System;

namespace Avalonia.Controls.Templates
{
    public static class FuncTemplateNameScopeExtensions
    {
        public static T RegisterInNameScope<T>(this T control, INameScope scope)
        where T : StyledElement
        {
            scope.Register(control.Name, control);
            return control;
        }

        public static T WithNameScope<T>(this T control, INameScope scope)
            where T : StyledElement
        {
            var existingScope = NameScope.GetNameScope(control);
            if (existingScope != null && existingScope != scope)
                throw new InvalidOperationException("Control already has a name scope");
            NameScope.SetNameScope(control, scope);
            return control;
        }
    }
}
