// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using Perspex.HtmlRenderer;
using Perspex.Media;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Core;
using TheArtOfDev.HtmlRenderer.Perspex.Adapters;

namespace Perspex.Controls
{
    /// <summary>
    /// Provides HTML rendering using the text property.<br/>
    /// WPF control that will render html content in it's client rectangle.<br/>
    /// Using <see cref="AutoSize"/> and <see cref="AutoSizeHeightOnly"/> client can control how the html content effects the
    /// size of the label. Either case scrollbars are never shown and html content outside of client bounds will be clipped.
    /// MaxWidth/MaxHeight and MinWidth/MinHeight with AutoSize can limit the max/min size of the control<br/>
    /// The control will handle mouse and keyboard events on it to support html text selection, copy-paste and mouse clicks.<br/>
    /// </summary>
    /// <remarks>
    /// See <see cref="HtmlControl"/> for more info.
    /// </remarks>
    public class HtmlLabel : HtmlControl
    {
        #region Dependency properties

        public static readonly PerspexProperty AutoSizeProperty = PropertyHelper.Register<HtmlLabel, bool>("AutoSize", true, OnPerspexProperty_valueChanged);
        public static readonly PerspexProperty AutoSizeHeightOnlyProperty = PropertyHelper.Register<HtmlLabel, bool>("AutoSizeHeightOnly", false, OnPerspexProperty_valueChanged);

        #endregion


        /// <summary>
        /// Init.
        /// </summary>
        static HtmlLabel()
        {
            //BackgroundProperty.OverrideDefaultValue<HtmlLabel>(Brushes.Transparent);
        }

        #region Private methods

        /// <summary>
        /// Perform the layout of the html in the control.
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            if (_htmlContainer != null)
            {
                using (var ig = new GraphicsAdapter())
                {
                    var horizontal = Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right;
                    var vertical = Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom;

                    var size = new RSize(constraint.Width < Double.PositiveInfinity ? constraint.Width - horizontal : 0, constraint.Height < Double.PositiveInfinity ? constraint.Height - vertical : 0);
                    //var minSize = new RSize(MinWidth < Double.PositiveInfinity ? MinWidth - horizontal : 0, MinHeight < Double.PositiveInfinity ? MinHeight - vertical : 0);
                    var maxSize = new RSize(MaxWidth < Double.PositiveInfinity ? MaxWidth - horizontal : 0, MaxHeight < Double.PositiveInfinity ? MaxHeight - vertical : 0);
                    _htmlContainer.HtmlContainerInt.MaxSize = maxSize;

                    _htmlContainer.HtmlContainerInt.PerformLayout(ig);
                    var newSize = _htmlContainer.ActualSize;
                    constraint = new Size(newSize.Width + horizontal, newSize.Height + vertical);
                }
            }

            if (double.IsPositiveInfinity(constraint.Width) || double.IsPositiveInfinity(constraint.Height))
                constraint = Size.Empty;

            return constraint;
        }

        /// <summary>
        /// Handle when dependency property value changes to update the underline HtmlContainer with the new value.
        /// </summary>
        private static void OnPerspexProperty_valueChanged(PerspexObject PerspexObject, PerspexPropertyChangedEventArgs e)
        {
            var control = PerspexObject as HtmlLabel;
            if (control != null)
            {
                if (e.Property == AutoSizeProperty)
                {
                    if ((bool)e.NewValue)
                    {
                        PerspexObject.SetValue(AutoSizeHeightOnlyProperty, false);
                        control.InvalidateMeasure();
                        control.InvalidateVisual();
                    }
                }
                else if (e.Property == AutoSizeHeightOnlyProperty)
                {
                    if ((bool)e.NewValue)
                    {
                        PerspexObject.SetValue(AutoSizeProperty, false);
                        control.InvalidateMeasure();
                        control.InvalidateVisual();
                    }
                }
            }
        }

        #endregion
    }
}