using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Avalonia.Markup.Xaml.Templates
{
    public static class TemplateContent
    {
        public static TemplateResult<Control>? Load(object? templateContent)
            => Load<Control>(templateContent);

        public static TemplateResult<T>? Load<T>(object? templateContent)
            => templateContent switch
            {
                IDeferredContent deferred => (TemplateResult<T>?)deferred.Build(null),
                Func<IServiceProvider?, object?> deferred => (TemplateResult<T>?)deferred(null),
                null => null,
                _ => throw new ArgumentException($"Unexpected content {templateContent.GetType()}", nameof(templateContent))
            };
    }
}
