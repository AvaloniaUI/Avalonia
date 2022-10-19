using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// Presents a color for user editing using a spectrum, palette and component sliders within a drop down.
    /// Editing is available when the drop down flyout is opened; otherwise, only the preview color is shown.
    /// </summary>
    public class ColorPicker : ColorView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPicker"/> class.
        /// </summary>
        public ColorPicker() : base()
        {
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            // Until this point the ColorPicker itself is responsible to process property updates.
            // This, for example, syncs Color with HsvColor and updates primitive controls.
            //
            // However, when the template is created, hand-off this change processing to the
            // ColorView within the control template itself. Remember ColorPicker derives from
            // ColorView so we don't want two instances of the same logic fighting each other.
            // It is best to hand-off to the ColorView in the control template because that is the
            // primary point of user-interaction for the overall control. It also simplifies binding.
            //
            // Keep in mind this hand-off is not possible until the template controls are created
            // which is done after the ColorPicker is instantiated. The ColorPicker must still
            // process updates before the template is applied to ensure all property changes in
            // XAML or object initializers are handled correctly. Otherwise, there can be bugs
            // such as setting the Color property doesn't work because the HsvColor is never updated
            // and then the Color value is lost once the template loads (and the template ColorView
            // takes over).
            //
            // In order to complete this hand-off, completely ignore property changes here in the
            // ColorPicker. This means the ColorView in the control template is now responsible to
            // process property changes and handle primary calculations.
            base.ignorePropertyChanged = true;
        }
    }
}
