using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Avalonia.Themes.Fluent2.UnitTests;

/// <summary>
/// Constructs every control that has an implicit Fluent2 theme inside a headless
/// window, runs layout and renders a frame. Catches template binding errors and
/// dangling resource references that key parity alone cannot see.
/// </summary>
public class InstantiationSmokeTests
{
    [AvaloniaFact]
    public void Every_themed_control_renders_under_fluent2()
    {
        var v1 = new Avalonia.Themes.Fluent.FluentTheme();
        var types = ThemeResourceWalker.CollectKeys(v1).OfType<Type>()
            .Where(t => typeof(Control).IsAssignableFrom(t)
                        && !typeof(TopLevel).IsAssignableFrom(t)
                        && !t.IsAbstract
                        && t.IsPublic
                        && t.GetConstructor(Type.EmptyTypes) is not null)
            .OrderBy(t => t.Name)
            .ToList();

        Assert.True(types.Count > 60, $"Only {types.Count} instantiable control types found.");

        var theme = new Fluent2Theme();
        var app = Application.Current!;
        app.Styles.Add(theme);
        var problems = new List<string>();
        try
        {
            foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
            {
                var window = new Window
                {
                    Width = 400,
                    Height = 300,
                    RequestedThemeVariant = variant,
                };
                window.Show();
                try
                {
                    foreach (var type in types)
                    {
                        try
                        {
                            var control = (Control)Activator.CreateInstance(type)!;
                            window.Content = control;
                            Dispatcher.UIThread.RunJobs();
                            window.CaptureRenderedFrame()?.Dispose();
                        }
                        catch (Exception e)
                        {
                            problems.Add($"{type.Name} ({variant}): {e.GetType().Name}: {e.Message}");
                        }
                    }
                }
                finally
                {
                    window.Close();
                }
            }
        }
        finally
        {
            app.Styles.Remove(theme);
        }

        Assert.True(problems.Count == 0,
            $"{problems.Count} controls failed to render:\n" + string.Join("\n", problems));
    }
}
