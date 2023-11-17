using System;
using System.Linq;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A layer that is used to dismiss a <see cref="Popup"/> when the user clicks outside.
    /// </summary>
    public class LightDismissOverlayLayer : Border, ICustomHitTest
    {
        public IInputElement? InputPassThroughElement { get; set; }

        static LightDismissOverlayLayer()
        {
            BackgroundProperty.OverrideDefaultValue<LightDismissOverlayLayer>(Brushes.Transparent);
        }

        /// <summary>
        /// Returns the light dismiss overlay for a specified visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The light dismiss overlay, or null if none found.</returns>
        public static LightDismissOverlayLayer? GetLightDismissOverlayLayer(Visual visual)
        {
            visual = visual ?? throw new ArgumentNullException(nameof(visual));

            VisualLayerManager? manager;

            if (visual is TopLevel topLevel)
            {
                manager = topLevel.GetTemplateChildren()
                    .OfType<VisualLayerManager>()
                    .FirstOrDefault();
            }
            else
            {
                manager = visual.FindAncestorOfType<VisualLayerManager>();
            }

            return manager?.LightDismissOverlayLayer;
        }

        /// <inheritdoc />
        public bool HitTest(Point point)
        {
            if (InputPassThroughElement is Visual v)
            {
                if (VisualRoot is IInputElement ie && ie.InputHitTest(point, x => x != this) is Visual hit)
                {
                    return !v.IsVisualAncestorOf(hit);
                }
            }

            return true;
        }
    }
}
