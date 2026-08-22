using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    internal class LightDismissOverlayLayer : Border, ICustomHitTest
    {
        private readonly List<Registration> _registrations = [];

        public IInputElement? InputPassThroughElement { get; private set; }

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
                manager = topLevel.GetTemplateDescendants()
                    .OfType<VisualLayerManager>()
                    .FirstOrDefault();
            }
            else
            {
                manager = visual.FindAncestorOfType<VisualLayerManager>();
            }

            return manager?.LightDismissOverlayLayer;
        }

        public IDisposable Register(IInputElement? inputPassThroughElement)
        {
            var registration = new Registration(this, inputPassThroughElement);
            _registrations.Add(registration);
            UpdateState();
            return registration;
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

        private void Unregister(Registration registration)
        {
            _registrations.Remove(registration);
            UpdateState();
        }

        private void UpdateState()
        {
            IsVisible = _registrations.Count > 0;
            InputPassThroughElement = _registrations.LastOrDefault()?.InputPassThroughElement;
        }

        private sealed class Registration(LightDismissOverlayLayer owner, IInputElement? inputPassThroughElement) : IDisposable
        {
            private LightDismissOverlayLayer? _owner = owner;

            public IInputElement? InputPassThroughElement { get; } = inputPassThroughElement;

            public void Dispose() => Interlocked.Exchange(ref _owner, null)?.Unregister(this);
        }
    }
}
