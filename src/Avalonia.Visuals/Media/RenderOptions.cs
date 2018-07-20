// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Visuals.Media.Imaging;

namespace Avalonia.Visuals.Media
{ 
    public class RenderOptions
    {
        /// <summary>
        /// Defines the <see cref="Imaging.BitmapInterpolationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<BitmapInterpolationMode> BitmapInterpolationMode =
            AvaloniaProperty.RegisterAttached<RenderOptions, AvaloniaObject, BitmapInterpolationMode>(
                "BitmapInterpolationMode",
                inherits: true);

        /// <summary>
        /// Gets the value of the BitmapInterpolationMode attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <returns>The control's left coordinate.</returns>
        public static BitmapInterpolationMode GetBitmapScalingMode(AvaloniaObject element)
        {
            return element.GetValue(BitmapInterpolationMode);
        }

        /// <summary>
        /// Sets the value of the BitmapInterpolationMode attached property for a control.
        /// </summary>
        /// <param name="element">The control.</param>
        /// <param name="value">The left value.</param>
        public static void SetBitmapScalingMode(AvaloniaObject element, BitmapInterpolationMode value)
        {
            element.SetValue(BitmapInterpolationMode, value);
        }
    }
}
