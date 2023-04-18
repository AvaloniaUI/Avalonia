using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Avalonia.Markup.Xaml.Templates
{
    public static class TemplateContent
    {
        public static TemplateResult<Control>? Load(object? templateContent)
        {
            if (templateContent is Func<IServiceProvider?, object?> direct)
            {
                return (TemplateResult<Control>?)direct(null);
            }

            if (templateContent is null)
            {
                return null;
            }

            throw new ArgumentException($"Unexpected content {templateContent.GetType()}", nameof(templateContent));
        }

        public static TemplateResult<T>? Load<T>(object? templateContent)
        {
            if (templateContent is Func<IServiceProvider?, object?> direct)
                return (TemplateResult<T>?)direct(null);

            if (templateContent is null)
                return null;

            throw new ArgumentException($"Unexpected content {templateContent.GetType()}", nameof(templateContent));
        }
    }
}
