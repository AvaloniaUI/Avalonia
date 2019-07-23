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
    }
}
