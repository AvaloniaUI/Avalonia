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

using Perspex.Controls;
using Perspex.Input;
using TheArtOfDev.HtmlRenderer.Adapters;
using TheArtOfDev.HtmlRenderer.Adapters.Entities;
using TheArtOfDev.HtmlRenderer.Core.Utils;
using TheArtOfDev.HtmlRenderer.Perspex.Utilities;
// ReSharper disable ConvertPropertyToExpressionBody

namespace TheArtOfDev.HtmlRenderer.Perspex.Adapters
{
    /// <summary>
    /// Adapter for Perspex Control for core.
    /// </summary>
    internal sealed class ControlAdapter : RControl
    {
        /// <summary>
        /// the underline Perspex control.
        /// </summary>
        private readonly Control _control;

        /// <summary>
        /// Init.
        /// </summary>
        public ControlAdapter(Control control)
            : base(PerspexAdapter.Instance)
        {
            ArgChecker.AssertArgNotNull(control, "control");

            _control = control;
            _control.PointerPressed += delegate { _leftMouseButton = true; };
            _control.PointerPressed += delegate { _leftMouseButton = false; };
        }

        /// <summary>
        /// Get the underline Perspex control
        /// </summary>
        public Control Control
        {
            get { return _control; }
        }

        public override RPoint MouseLocation
        {
            get
            {
                return Util.Convert(MouseDevice.Instance.GetPosition(_control));
            }
        }

        private bool _leftMouseButton;
        public override bool LeftMouseButton => _leftMouseButton;

        public override bool RightMouseButton
        {
            get
            {
                return false;
                //TODO: Implement right mouse click
                //return Mouse.RightButton == MouseButtonState.Pressed;
            }
        }

        public override void SetCursorDefault()
        {
            _control.Cursor = new Cursor(StandardCursorType.Arrow);
        }

        public override void SetCursorHand()
        {
            _control.Cursor = new Cursor(StandardCursorType.Hand);
        }

        public override void SetCursorIBeam()
        {
            _control.Cursor = new Cursor(StandardCursorType.Ibeam);
        }

        public override void DoDragDropCopy(object dragDropData)
        {
            //TODO: Implement DragDropCopy
            //DragDrop.DoDragDrop(_control, dragDropData, DragDropEffects.Copy);
        }

        public override void MeasureString(string str, RFont font, double maxWidth, out int charFit, out double charFitWidth)
        {
            using (var g = new GraphicsAdapter())
            {
                g.MeasureString(str, font, maxWidth, out charFit, out charFitWidth);
            }
        }

        public override void Invalidate()
        {
            _control.InvalidateVisual();
        }
    }
}