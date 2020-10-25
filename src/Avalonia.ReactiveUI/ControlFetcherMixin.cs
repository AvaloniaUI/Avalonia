using System;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Logging;

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// ControlFetcherMixin helps you automatically wire-up Avalonia controls via property names,
    /// based on x:Name directives declared in an Avalonia XAMl file similar to Butter Knife.
    /// </summary>
    public static class ControlFetcherMixin
    {
        /// <summary>
        /// Initializes all public properties with public setters of the given <see cref="IControl"/>
        /// by looking for x:Name directives in the corresponding Avalonia XAML file.
        /// </summary>
        /// <param name="target">The control containing x:Name directives in its XAML markup.</param>
        /// <exception cref="ArgumentNullException">Thrown when the target control is null.</exception>
        /// <exception cref="MissingFieldException">
        /// Thrown when we are unable to find a control with the x:Name directive in the XAML file.
        /// </exception>
        public static void WireUpControls(this IControl target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var members = target
                .GetType()
                .GetRuntimeProperties()
                .Where(member => typeof(IControl).IsAssignableFrom(member.PropertyType))
                .ToList();

            foreach (var member in members)
            {
                try
                {
                    var view = target.FindControl<IControl>(member.Name);
                    member.SetValue(target, view);
                }
                catch (Exception exception)
                {
                    Logger.TryGet(LogEventLevel.Debug, "WireUpControls")?.Log(
                        target,
                        "Failed to wire up the Property {0} to a View with a corresponding identifier: {1}",
                        member.Name,
                        exception);
                }
            }
        }
    }
}
