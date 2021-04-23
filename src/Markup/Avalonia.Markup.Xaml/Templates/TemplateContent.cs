using System;
using Avalonia.Controls.Templates;

namespace Avalonia.Markup.Xaml.Templates
{
    
    public static class TemplateContent
    {
        public static ControlTemplateResult Load(object templateContent)
        {
            if (templateContent is Func<IServiceProvider, object> direct)
            {
                return (ControlTemplateResult)direct(null);
            }

            if (templateContent is null)
            {
                return null;
            }

            throw new ArgumentException(nameof(templateContent));
        }
    }
}
