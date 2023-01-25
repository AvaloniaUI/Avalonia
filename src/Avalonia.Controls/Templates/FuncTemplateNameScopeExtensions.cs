using System;

namespace Avalonia.Controls.Templates
{
    public static class FuncTemplateNameScopeExtensions
    {
        public static T RegisterInNameScope<T>(this T control, INameScope scope)
        where T : StyledElement
        {
            if (control.Name is null)
                throw new ArgumentException("RegisterInNameScope must be called on a control with non-null name.");

            scope.Register(control.Name, control);
            return control;
        }
    }
}
