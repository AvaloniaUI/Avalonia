// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A two dimensional spectrum for color selection.
    /// </summary>
    [PseudoClasses(pcLargeSelector, pcLightSelector)]
    public partial class ColorSpectrum : TemplatedControl
    {
        protected const string pcLargeSelector = ":large-selector";
        protected const string pcLightSelector = ":light-selector";

        /// <summary>
        /// Event for when the selected color changes within the spectrum.
        /// </summary>
        public event EventHandler<ColorChangedEventArgs>? ColorChanged;

        private bool _updatingColor = false;
        private bool _updatingHsvColor = false;
        private bool _isPointerOver = false;
        private bool _isPointerPressed = false;
        private bool _shouldShowLargeSelection = false;
        private List<Hsv> _hsvValues = new List<Hsv>();

        private IDisposable? _layoutRootDisposable;

        // XAML template parts
        private Grid? _layoutRoot;
        private Grid? _sizingGrid;
        private Rectangle? _spectrumRectangle;
        private Ellipse? _spectrumEllipse;
        private Rectangle? _spectrumOverlayRectangle;
        private Ellipse? _spectrumOverlayEllipse;
        private Canvas? _inputTarget;
        private Panel? _selectionEllipsePanel;
        private ToolTip? _colorNameToolTip;

        // Put the spectrum images in a bitmap, which is then given to an ImageBrush.
        private WriteableBitmap? _hueRedBitmap;
        private WriteableBitmap? _hueYellowBitmap;
        private WriteableBitmap? _hueGreenBitmap;
        private WriteableBitmap? _hueCyanBitmap;
        private WriteableBitmap? _hueBlueBitmap;
        private WriteableBitmap? _huePurpleBitmap;

        private WriteableBitmap? _saturationMinimumBitmap;
        private WriteableBitmap? _saturationMaximumBitmap;

        private WriteableBitmap? _valueBitmap;

        // Fields used by UpdateEllipse() to ensure that it's using the data
        // associated with the last call to CreateBitmapsAndColorMap(),
        // in order to function properly while the asynchronous bitmap creation
        // is in progress.
        private ColorSpectrumShape _shapeFromLastBitmapCreation = ColorSpectrumShape.Box;
        private ColorSpectrumChannels _componentsFromLastBitmapCreation = ColorSpectrumChannels.HueSaturation;
        private double _imageWidthFromLastBitmapCreation = 0.0;
        private double _imageHeightFromLastBitmapCreation = 0.0;
        private int _minHueFromLastBitmapCreation = 0;
        private int _maxHueFromLastBitmapCreation = 0;
        private int _minSaturationFromLastBitmapCreation = 0;
        private int _maxSaturationFromLastBitmapCreation = 0;
        private int _minValueFromLastBitmapCreation = 0;
        private int _maxValueFromLastBitmapCreation = 0;

        private Color _oldColor = Color.FromArgb(255, 255, 255, 255);
        private HsvColor _oldHsvColor = HsvColor.FromAhsv(0.0f, 0.0f, 1.0f, 1.0f);

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorSpectrum"/> class.
        /// </summary>
        public ColorSpectrum()
        {
            _shapeFromLastBitmapCreation = Shape;
            _componentsFromLastBitmapCreation = Channels;
            _imageWidthFromLastBitmapCreation = 0;
            _imageHeightFromLastBitmapCreation = 0;
            _minHueFromLastBitmapCreation = MinHue;
            _maxHueFromLastBitmapCreation = MaxHue;
            _minSaturationFromLastBitmapCreation = MinSaturation;
            _maxSaturationFromLastBitmapCreation = MaxSaturation;
            _minValueFromLastBitmapCreation = MinValue;
            _maxValueFromLastBitmapCreation = MaxValue;
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            UnregisterEvents();

            _layoutRoot = e.NameScope.Find<Grid>("PART_LayoutRoot");
            _sizingGrid = e.NameScope.Find<Grid>("PART_SizingGrid");
            _spectrumRectangle = e.NameScope.Find<Rectangle>("PART_SpectrumRectangle");
            _spectrumEllipse = e.NameScope.Find<Ellipse>("PART_SpectrumEllipse");
            _spectrumOverlayRectangle = e.NameScope.Find<Rectangle>("PART_SpectrumOverlayRectangle");
            _spectrumOverlayEllipse = e.NameScope.Find<Ellipse>("PART_SpectrumOverlayEllipse");
            _inputTarget = e.NameScope.Find<Canvas>("PART_InputTarget");
            _selectionEllipsePanel = e.NameScope.Find<Panel>("PART_SelectionEllipsePanel");
            _colorNameToolTip = e.NameScope.Find<ToolTip>("PART_ColorNameToolTip");

            if (_layoutRoot != null)
            {
                _layoutRootDisposable = _layoutRoot.GetObservable(BoundsProperty).Subscribe(_ => OnLayoutRootSizeChanged());
            }

            if (_inputTarget != null)
            {
                _inputTarget.PointerEnter += OnInputTargetPointerEnter;
                _inputTarget.PointerLeave += OnInputTargetPointerLeave;
                _inputTarget.PointerPressed += OnInputTargetPointerPressed;
                _inputTarget.PointerMoved += OnInputTargetPointerMoved;
                _inputTarget.PointerReleased += OnInputTargetPointerReleased;
            }

            if (ColorHelpers.ToDisplayNameExists)
            {
                if (_colorNameToolTip != null)
                {
                    _colorNameToolTip.Content = ColorHelpers.ToDisplayName(Color);
                }
            }

            if (_selectionEllipsePanel != null)
            {
                // TODO: After FlowDirection PR is merged: https://github.com/AvaloniaUI/Avalonia/pull/7810
                //m_selectionEllipsePanel.RegisterPropertyChangedCallback(FrameworkElement.FlowDirectionProperty, OnSelectionEllipseFlowDirectionChanged);
            }

            // If we haven't yet created our bitmaps, do so now.
            if (_hsvValues.Count == 0)
            {
                CreateBitmapsAndColorMap();
            }

            UpdateEllipse();
            UpdatePseudoClasses();
        }

        /// <summary>
        /// Explicitly unregisters all events connected in OnApplyTemplate().
        /// </summary>
        private void UnregisterEvents()
        {
            _layoutRootDisposable?.Dispose();
            _layoutRootDisposable = null;

            if (_inputTarget != null)
            {
                _inputTarget.PointerEnter -= OnInputTargetPointerEnter;
                _inputTarget.PointerLeave -= OnInputTargetPointerLeave;
                _inputTarget.PointerPressed -= OnInputTargetPointerPressed;
                _inputTarget.PointerMoved -= OnInputTargetPointerMoved;
                _inputTarget.PointerReleased -= OnInputTargetPointerReleased;
            }
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            var key = e.Key;

            if (key != Key.Left &&
                key != Key.Right &&
                key != Key.Up &&
                key != Key.Down)
            {
                base.OnKeyDown(e);
                return;
            }

            bool isControlDown = e.KeyModifiers.HasFlag(KeyModifiers.Control);

            HsvChannel incrementChannel = HsvChannel.Hue;

            if (key == Key.Left ||
                key == Key.Right)
            {
                switch (Channels)
                {
                    case ColorSpectrumChannels.HueSaturation:
                    case ColorSpectrumChannels.HueValue:
                        incrementChannel = HsvChannel.Hue;
                        break;

                    case ColorSpectrumChannels.SaturationHue:
                    case ColorSpectrumChannels.SaturationValue:
                        incrementChannel = HsvChannel.Saturation;
                        break;

                    case ColorSpectrumChannels.ValueHue:
                    case ColorSpectrumChannels.ValueSaturation:
                        incrementChannel = HsvChannel.Value;
                        break;
                }
            }
            else if (key == Key.Up ||
                     key == Key.Down)
            {
                switch (Channels)
                {
                    case ColorSpectrumChannels.SaturationHue:
                    case ColorSpectrumChannels.ValueHue:
                        incrementChannel = HsvChannel.Hue;
                        break;

                    case ColorSpectrumChannels.HueSaturation:
                    case ColorSpectrumChannels.ValueSaturation:
                        incrementChannel = HsvChannel.Saturation;
                        break;

                    case ColorSpectrumChannels.HueValue:
                    case ColorSpectrumChannels.SaturationValue:
                        incrementChannel = HsvChannel.Value;
                        break;
                }
            }

            double minBound = 0.0;
            double maxBound = 0.0;

            switch (incrementChannel)
            {
                case HsvChannel.Hue:
                    minBound = MinHue;
                    maxBound = MaxHue;
                    break;

                case HsvChannel.Saturation:
                    minBound = MinSaturation;
                    maxBound = MaxSaturation;
                    break;

                case HsvChannel.Value:
                    minBound = MinValue;
                    maxBound = MaxValue;
                    break;
            }

            // The order of saturation and value in the spectrum is reversed - the max value is at the bottom while the min value is at the top -
            // so we want left and up to be lower for hue, but higher for saturation and value.
            // This will ensure that the icon always moves in the direction of the key press.
            IncrementDirection direction =
                (incrementChannel == HsvChannel.Hue && (key == Key.Left || key == Key.Up)) ||
                (incrementChannel != HsvChannel.Hue && (key == Key.Right || key == Key.Down)) ?
                IncrementDirection.Lower :
                IncrementDirection.Higher;

            IncrementAmount amount = isControlDown ? IncrementAmount.Large : IncrementAmount.Small;

            HsvColor hsvColor = HsvColor;
            UpdateColor(ColorHelpers.IncrementColorChannel(
                new Hsv(hsvColor),
                incrementChannel,
                direction,
                amount,
                shouldWrap: true,
                minBound,
                maxBound));

            e.Handled = true;

            return;
        }

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            // We only want to bother with the color name tool tip if we can provide color names.
            if (_colorNameToolTip is ToolTip colorNameToolTip)
            {
                if (ColorHelpers.ToDisplayNameExists)
                {
                    //colorNameToolTip.IsOpen = true;
                }
            }

            UpdatePseudoClasses();
        }

        /// <inheritdoc/>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            // We only want to bother with the color name tool tip if we can provide color names.
            if (_colorNameToolTip is ToolTip colorNameToolTip)
            {
                if (ColorHelpers.ToDisplayNameExists)
                {
                    //colorNameToolTip.IsOpen = false;
                }
            }

            UpdatePseudoClasses();
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == ColorProperty)
            {
                OnColorChanged(change);
            }
            else if (change.Property == HsvColorProperty)
            {
                OnHsvColorChanged(change);
            }
            else if (
                change.Property == MinHueProperty ||
                change.Property == MaxHueProperty)
            {
                OnMinMaxHueChanged();
            }
            else if (
                change.Property == MinSaturationProperty ||
                change.Property == MaxSaturationProperty)
            {
                OnMinMaxSaturationChanged();
            }
            else if (
                change.Property == MinValueProperty ||
                change.Property == MaxValueProperty)
            {
                OnMinMaxValueChanged();
            }
            else if (change.Property == ShapeProperty)
            {
                OnShapeChanged();
            }
            else if (change.Property == ChannelsProperty)
            {
                OnComponentsChanged();
            }

            base.OnPropertyChanged(change);
        }

        private void OnColorChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            // If we're in the process of internally updating the color,
            // then we don't want to respond to the Color property changing.
            if (!_updatingColor)
            {
                Color color = Color;

                _updatingHsvColor = true;
                Hsv newHsv = (new Rgb(color)).ToHsv();
                HsvColor = newHsv.ToHsvColor(color.A / 255.0);
                _updatingHsvColor = false;

                UpdateEllipse();
                UpdateBitmapSources();
            }

            _oldColor = change.OldValue.GetValueOrDefault<Color>();
        }

        private void OnHsvColorChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            // If we're in the process of internally updating the HSV color,
            // then we don't want to respond to the HsvColor property changing.
            if (!_updatingHsvColor)
            {
                SetColor();
            }

            _oldHsvColor = change.OldValue.GetValueOrDefault<HsvColor>();
        }

        private void SetColor()
        {
            HsvColor hsvColor = HsvColor;

            _updatingColor = true;
            Rgb newRgb = (new Hsv(hsvColor)).ToRgb();

            Color = newRgb.ToColor(hsvColor.A);

            _updatingColor = false;

            UpdateEllipse();
            UpdateBitmapSources();
            RaiseColorChanged();
        }

        public void RaiseColorChanged()
        {
            Color newColor = Color;

            if (_oldColor.A != newColor.A ||
                _oldColor.R != newColor.R ||
                _oldColor.G != newColor.G ||
                _oldColor.B != newColor.B)
            {
                var colorChangedEventArgs = new ColorChangedEventArgs();

                colorChangedEventArgs.OldColor = _oldColor;
                colorChangedEventArgs.NewColor = newColor;

                ColorChanged?.Invoke(this, colorChangedEventArgs);

                if (ColorHelpers.ToDisplayNameExists)
                {
                    if (_colorNameToolTip is ToolTip colorNameToolTip)
                    {
                        colorNameToolTip.Content = ColorHelpers.ToDisplayName(newColor);
                    }
                }
            }
        }

        protected void OnMinMaxHueChanged()
        {
            int minHue = MinHue;
            int maxHue = MaxHue;

            if (minHue < 0 || minHue > 359)
            {
                throw new ArgumentException("MinHue must be between 0 and 359.");
            }
            else if (maxHue < 0 || maxHue > 359)
            {
                throw new ArgumentException("MaxHue must be between 0 and 359.");
            }

            ColorSpectrumChannels channels = Channels;

            // If hue is one of the axes in the spectrum bitmap, then we'll need to regenerate it
            // if the maximum or minimum value has changed.
            if (channels != ColorSpectrumChannels.SaturationValue &&
                channels != ColorSpectrumChannels.ValueSaturation)
            {
                CreateBitmapsAndColorMap();
            }
        }

        protected void OnMinMaxSaturationChanged()
        {
            int minSaturation = MinSaturation;
            int maxSaturation = MaxSaturation;

            if (minSaturation < 0 || minSaturation > 100)
            {
                throw new ArgumentException("MinSaturation must be between 0 and 100.");
            }
            else if (maxSaturation < 0 || maxSaturation > 100)
            {
                throw new ArgumentException("MaxSaturation must be between 0 and 100.");
            }

            ColorSpectrumChannels channels = Channels;

            // If value is one of the axes in the spectrum bitmap, then we'll need to regenerate it
            // if the maximum or minimum value has changed.
            if (channels != ColorSpectrumChannels.HueValue &&
                channels != ColorSpectrumChannels.ValueHue)
            {
                CreateBitmapsAndColorMap();
            }
        }

        private void OnMinMaxValueChanged()
        {
            int minValue = MinValue;
            int maxValue = MaxValue;

            if (minValue < 0 || minValue > 100)
            {
                throw new ArgumentException("MinValue must be between 0 and 100.");
            }
            else if (maxValue < 0 || maxValue > 100)
            {
                throw new ArgumentException("MaxValue must be between 0 and 100.");
            }

            ColorSpectrumChannels channels = Channels;

            // If value is one of the axes in the spectrum bitmap, then we'll need to regenerate it
            // if the maximum or minimum value has changed.
            if (channels != ColorSpectrumChannels.HueSaturation &&
                channels != ColorSpectrumChannels.SaturationHue)
            {
                CreateBitmapsAndColorMap();
            }
        }

        private void OnShapeChanged()
        {
            CreateBitmapsAndColorMap();
        }

        private void OnComponentsChanged()
        {
            CreateBitmapsAndColorMap();
        }

        /// <summary>
        /// Updates the visual state of the control by applying latest PseudoClasses.
        /// </summary>
        private void UpdatePseudoClasses()
        {
            if (_isPointerPressed)
            {
                PseudoClasses.Set(pcLargeSelector, _shouldShowLargeSelection);
            }
            else if (_isPointerOver)
            {
                //VisualStateManager.GoToState(this, "PointerOver", useTransitions);
                PseudoClasses.Set(pcLargeSelector, false);
            }
            else
            {
                PseudoClasses.Set(pcLargeSelector, false);
            }

            PseudoClasses.Set(pcLightSelector, SelectionEllipseShouldBeLight());

            //if (IsEnabled && FocusState != FocusState.Unfocused)
            //{
            //    if (FocusState == FocusState.Pointer)
            //    {
            //        VisualStateManager.GoToState(this, "PointerFocused", useTransitions);
            //    }
            //    else
            //    {
            //        VisualStateManager.GoToState(this, "Focused", useTransitions);
            //    }
            //}
            //else
            //{
            //    VisualStateManager.GoToState(this, "Unfocused", useTransitions);
            //}
        }

        private void UpdateColor(Hsv newHsv)
        {
            _updatingColor = true;
            _updatingHsvColor = true;

            Rgb newRgb = newHsv.ToRgb();
            double alpha = HsvColor.A;

            Color = newRgb.ToColor(alpha);
            HsvColor = newHsv.ToHsvColor(alpha);

            UpdateEllipse();
            UpdatePseudoClasses();

            _updatingHsvColor = false;
            _updatingColor = false;

            RaiseColorChanged();
        }

        private void UpdateColorFromPoint(PointerPoint point)
        {
            // If we haven't initialized our HSV value array yet, then we should just ignore any user input -
            // we don't yet know what to do with it.
            if (_hsvValues.Count == 0)
            {
                return;
            }

            double xPosition = point.Position.X;
            double yPosition = point.Position.Y;
            double radius = Math.Min(_imageWidthFromLastBitmapCreation, _imageHeightFromLastBitmapCreation) / 2;
            double distanceFromRadius = Math.Sqrt(Math.Pow(xPosition - radius, 2) + Math.Pow(yPosition - radius, 2));

            var shape = Shape;

            // If the point is outside the circle, we should bring it back into the circle.
            if (distanceFromRadius > radius && shape == ColorSpectrumShape.Ring)
            {
                xPosition = (radius / distanceFromRadius) * (xPosition - radius) + radius;
                yPosition = (radius / distanceFromRadius) * (yPosition - radius) + radius;
            }

            // Now we need to find the index into the array of HSL values at each point in the spectrum m_image.
            int x = (int)Math.Round(xPosition);
            int y = (int)Math.Round(yPosition);
            int width = (int)Math.Round(_imageWidthFromLastBitmapCreation);

            if (x < 0)
            {
                x = 0;
            }
            else if (x >= _imageWidthFromLastBitmapCreation)
            {
                x = (int)Math.Round(_imageWidthFromLastBitmapCreation) - 1;
            }

            if (y < 0)
            {
                y = 0;
            }
            else if (y >= _imageHeightFromLastBitmapCreation)
            {
                y = (int)Math.Round(_imageHeightFromLastBitmapCreation) - 1;
            }

            // The gradient image contains two dimensions of HSL information, but not the third.
            // We should keep the third where it already was.
            // Note: This can sometimes cause a crash -- possibly due to differences in c# rounding. Therefore, index is now clamped.
            Hsv hsvAtPoint = _hsvValues[MathUtilities.Clamp((y * width + x), 0, _hsvValues.Count - 1)];

            var channels = Channels;
            var hsvColor = HsvColor;

            switch (channels)
            {
                case ColorSpectrumChannels.HueValue:
                case ColorSpectrumChannels.ValueHue:
                    hsvAtPoint.S = hsvColor.S;
                    break;

                case ColorSpectrumChannels.HueSaturation:
                case ColorSpectrumChannels.SaturationHue:
                    hsvAtPoint.V = hsvColor.V;
                    break;

                case ColorSpectrumChannels.ValueSaturation:
                case ColorSpectrumChannels.SaturationValue:
                    hsvAtPoint.H = hsvColor.H;
                    break;
            }

            UpdateColor(hsvAtPoint);
        }

        private void UpdateEllipse()
        {
            var selectionEllipsePanel = _selectionEllipsePanel;

            if (selectionEllipsePanel == null)
            {
                return;
            }

            // If we don't have an image size yet, we shouldn't be showing the ellipse.
            if (_imageWidthFromLastBitmapCreation == 0 ||
                _imageHeightFromLastBitmapCreation == 0)
            {
                selectionEllipsePanel.IsVisible = false;
                return;
            }
            else
            {
                selectionEllipsePanel.IsVisible = true;
            }

            double xPosition;
            double yPosition;

            Hsv hsvColor = new Hsv(HsvColor);

            hsvColor.H = MathUtilities.Clamp(hsvColor.H, (double)_minHueFromLastBitmapCreation, (double)_maxHueFromLastBitmapCreation);
            hsvColor.S = MathUtilities.Clamp(hsvColor.S, _minSaturationFromLastBitmapCreation / 100.0, _maxSaturationFromLastBitmapCreation / 100.0);
            hsvColor.V = MathUtilities.Clamp(hsvColor.V, _minValueFromLastBitmapCreation / 100.0, _maxValueFromLastBitmapCreation / 100.0);

            if (_shapeFromLastBitmapCreation == ColorSpectrumShape.Box)
            {
                double xPercent = 0;
                double yPercent = 0;

                double hPercent = (hsvColor.H - _minHueFromLastBitmapCreation) / (_maxHueFromLastBitmapCreation - _minHueFromLastBitmapCreation);
                double sPercent = (hsvColor.S * 100.0 - _minSaturationFromLastBitmapCreation) / (_maxSaturationFromLastBitmapCreation - _minSaturationFromLastBitmapCreation);
                double vPercent = (hsvColor.V * 100.0 - _minValueFromLastBitmapCreation) / (_maxValueFromLastBitmapCreation - _minValueFromLastBitmapCreation);

                // In the case where saturation was an axis in the spectrum with hue, or value is an axis, full stop,
                // we inverted the direction of that axis in order to put more hue on the outside of the ring,
                // so we need to do similarly here when positioning the ellipse.
                if (_componentsFromLastBitmapCreation == ColorSpectrumChannels.HueSaturation ||
                    _componentsFromLastBitmapCreation == ColorSpectrumChannels.SaturationHue)
                {
                    sPercent = 1 - sPercent;
                }
                else
                {
                    vPercent = 1 - vPercent;
                }

                switch (_componentsFromLastBitmapCreation)
                {
                    case ColorSpectrumChannels.HueValue:
                        xPercent = hPercent;
                        yPercent = vPercent;
                        break;

                    case ColorSpectrumChannels.HueSaturation:
                        xPercent = hPercent;
                        yPercent = sPercent;
                        break;

                    case ColorSpectrumChannels.ValueHue:
                        xPercent = vPercent;
                        yPercent = hPercent;
                        break;

                    case ColorSpectrumChannels.ValueSaturation:
                        xPercent = vPercent;
                        yPercent = sPercent;
                        break;

                    case ColorSpectrumChannels.SaturationHue:
                        xPercent = sPercent;
                        yPercent = hPercent;
                        break;

                    case ColorSpectrumChannels.SaturationValue:
                        xPercent = sPercent;
                        yPercent = vPercent;
                        break;
                }

                xPosition = _imageWidthFromLastBitmapCreation * xPercent;
                yPosition = _imageHeightFromLastBitmapCreation * yPercent;
            }
            else
            {
                double thetaValue = 0;
                double rValue = 0;

                double hThetaValue =
                    _maxHueFromLastBitmapCreation != _minHueFromLastBitmapCreation ?
                    360 * (hsvColor.H - _minHueFromLastBitmapCreation) / (_maxHueFromLastBitmapCreation - _minHueFromLastBitmapCreation) :
                    0;
                double sThetaValue =
                    _maxSaturationFromLastBitmapCreation != _minSaturationFromLastBitmapCreation ?
                    360 * (hsvColor.S * 100.0 - _minSaturationFromLastBitmapCreation) / (_maxSaturationFromLastBitmapCreation - _minSaturationFromLastBitmapCreation) :
                    0;
                double vThetaValue =
                    _maxValueFromLastBitmapCreation != _minValueFromLastBitmapCreation ?
                    360 * (hsvColor.V * 100.0 - _minValueFromLastBitmapCreation) / (_maxValueFromLastBitmapCreation - _minValueFromLastBitmapCreation) :
                    0;
                double hRValue = _maxHueFromLastBitmapCreation != _minHueFromLastBitmapCreation ?
                    (hsvColor.H - _minHueFromLastBitmapCreation) / (_maxHueFromLastBitmapCreation - _minHueFromLastBitmapCreation) - 1 :
                    0;
                double sRValue = _maxSaturationFromLastBitmapCreation != _minSaturationFromLastBitmapCreation ?
                    (hsvColor.S * 100.0 - _minSaturationFromLastBitmapCreation) / (_maxSaturationFromLastBitmapCreation - _minSaturationFromLastBitmapCreation) - 1 :
                    0;
                double vRValue = _maxValueFromLastBitmapCreation != _minValueFromLastBitmapCreation ?
                    (hsvColor.V * 100.0 - _minValueFromLastBitmapCreation) / (_maxValueFromLastBitmapCreation - _minValueFromLastBitmapCreation) - 1 :
                    0;

                // In the case where saturation was an axis in the spectrum with hue, or value is an axis, full stop,
                // we inverted the direction of that axis in order to put more hue on the outside of the ring,
                // so we need to do similarly here when positioning the ellipse.
                if (_componentsFromLastBitmapCreation == ColorSpectrumChannels.HueSaturation ||
                    _componentsFromLastBitmapCreation == ColorSpectrumChannels.ValueHue)
                {
                    sThetaValue = 360 - sThetaValue;
                    sRValue = -sRValue - 1;
                }
                else
                {
                    vThetaValue = 360 - vThetaValue;
                    vRValue = -vRValue - 1;
                }

                switch (_componentsFromLastBitmapCreation)
                {
                    case ColorSpectrumChannels.HueValue:
                        thetaValue = hThetaValue;
                        rValue = vRValue;
                        break;

                    case ColorSpectrumChannels.HueSaturation:
                        thetaValue = hThetaValue;
                        rValue = sRValue;
                        break;

                    case ColorSpectrumChannels.ValueHue:
                        thetaValue = vThetaValue;
                        rValue = hRValue;
                        break;

                    case ColorSpectrumChannels.ValueSaturation:
                        thetaValue = vThetaValue;
                        rValue = sRValue;
                        break;

                    case ColorSpectrumChannels.SaturationHue:
                        thetaValue = sThetaValue;
                        rValue = hRValue;
                        break;

                    case ColorSpectrumChannels.SaturationValue:
                        thetaValue = sThetaValue;
                        rValue = vRValue;
                        break;
                }

                double radius = Math.Min(_imageWidthFromLastBitmapCreation, _imageHeightFromLastBitmapCreation) / 2;

                xPosition = (Math.Cos((thetaValue * Math.PI / 180.0) + Math.PI) * radius * rValue) + radius;
                yPosition = (Math.Sin((thetaValue * Math.PI / 180.0) + Math.PI) * radius * rValue) + radius;
            }

            Canvas.SetLeft(selectionEllipsePanel, xPosition - (selectionEllipsePanel.Width / 2));
            Canvas.SetTop(selectionEllipsePanel, yPosition - (selectionEllipsePanel.Height / 2));

            // We only want to bother with the color name tool tip if we can provide color names.
            if (ColorHelpers.ToDisplayNameExists)
            {
                if (_colorNameToolTip is ToolTip colorNameToolTip)
                {
                    // ToolTip doesn't currently provide any way to re-run its placement logic if its placement target moves,
                    // so toggling IsEnabled induces it to do that without incurring any visual glitches.
                    colorNameToolTip.IsEnabled = false;
                    colorNameToolTip.IsEnabled = true;
                }
            }

            UpdatePseudoClasses();
        }

        private void OnLayoutRootSizeChanged()
        {
            CreateBitmapsAndColorMap();
        }

        private void OnInputTargetPointerEnter(object? sender, PointerEventArgs args)
        {
            _isPointerOver = true;
            UpdatePseudoClasses();
            args.Handled = true;
        }

        private void OnInputTargetPointerLeave(object? sender, PointerEventArgs args)
        {
            _isPointerOver = false;
            UpdatePseudoClasses();
            args.Handled = true;
        }

        private void OnInputTargetPointerPressed(object? sender, PointerPressedEventArgs args)
        {
            var inputTarget = _inputTarget;

            Focus();

            _isPointerPressed = true;
            _shouldShowLargeSelection =
                // TODO: After Pen PR is merged: https://github.com/AvaloniaUI/Avalonia/pull/7412
                // args.Pointer.Type == PointerType.Pen ||
                args.Pointer.Type == PointerType.Touch;

            args.Pointer.Capture(inputTarget);
            UpdateColorFromPoint(args.GetCurrentPoint(inputTarget));
            UpdatePseudoClasses();
            UpdateEllipse();

            args.Handled = true;
        }

        private void OnInputTargetPointerMoved(object? sender, PointerEventArgs args)
        {
            if (!_isPointerPressed)
            {
                return;
            }

            UpdateColorFromPoint(args.GetCurrentPoint(_inputTarget));
            args.Handled = true;
        }

        private void OnInputTargetPointerReleased(object? sender, PointerReleasedEventArgs args)
        {
            _isPointerPressed = false;
            _shouldShowLargeSelection = false;

            args.Pointer.Capture(null);
            UpdatePseudoClasses();
            UpdateEllipse();

            args.Handled = true;
        }

        // TODO: After FlowDirection PR is merged: https://github.com/AvaloniaUI/Avalonia/pull/7810
        //private void OnSelectionEllipseFlowDirectionChanged(DependencyObject o, DependencyProperty p)
        //{
        //    UpdateEllipse();
        //}

        private async void CreateBitmapsAndColorMap()
        {
            if (_layoutRoot == null ||
                _sizingGrid == null ||
                _inputTarget == null ||
                _spectrumRectangle == null ||
                _spectrumEllipse == null ||
                _spectrumOverlayRectangle == null ||
                _spectrumOverlayEllipse == null
                /*|| SharedHelpers.IsInDesignMode*/)
            {
                return;
            }

            var layoutRoot = _layoutRoot;
            var sizingGrid = _sizingGrid;
            var inputTarget = _inputTarget;
            var spectrumRectangle = _spectrumRectangle;
            var spectrumEllipse = _spectrumEllipse;
            var spectrumOverlayRectangle = _spectrumOverlayRectangle;
            var spectrumOverlayEllipse = _spectrumOverlayEllipse;

            // We want ColorSpectrum to always be a square, so we'll take the smaller of the dimensions
            // and size the sizing grid to that.
            double minDimension = Math.Min(layoutRoot.Bounds.Width, layoutRoot.Bounds.Height);

            if (minDimension == 0)
            {
                return;
            }

            sizingGrid.Width = minDimension;
            sizingGrid.Height = minDimension;

            if (sizingGrid.Clip is RectangleGeometry clip)
            {
                clip.Rect = new Rect(0, 0, minDimension, minDimension);
            }

            inputTarget.Width = minDimension;
            inputTarget.Height = minDimension;
            spectrumRectangle.Width = minDimension;
            spectrumRectangle.Height = minDimension;
            spectrumEllipse.Width = minDimension;
            spectrumEllipse.Height = minDimension;
            spectrumOverlayRectangle.Width = minDimension;
            spectrumOverlayRectangle.Height = minDimension;
            spectrumOverlayEllipse.Width = minDimension;
            spectrumOverlayEllipse.Height = minDimension;

            HsvColor hsvColor = HsvColor;
            int minHue = MinHue;
            int maxHue = MaxHue;
            int minSaturation = MinSaturation;
            int maxSaturation = MaxSaturation;
            int minValue = MinValue;
            int maxValue = MaxValue;
            ColorSpectrumShape shape = Shape;
            ColorSpectrumChannels channels = Channels;

            // If min >= max, then by convention, min is the only number that a property can have.
            if (minHue >= maxHue)
            {
                maxHue = minHue;
            }

            if (minSaturation >= maxSaturation)
            {
                maxSaturation = minSaturation;
            }

            if (minValue >= maxValue)
            {
                maxValue = minValue;
            }

            Hsv hsv = new Hsv(hsvColor);

            // The middle 4 are only needed and used in the case of hue as the third dimension.
            // Saturation and luminosity need only a min and max.
            List<byte> bgraMinPixelData = new List<byte>();
            List<byte> bgraMiddle1PixelData = new List<byte>();
            List<byte> bgraMiddle2PixelData = new List<byte>();
            List<byte> bgraMiddle3PixelData = new List<byte>();
            List<byte> bgraMiddle4PixelData = new List<byte>();
            List<byte> bgraMaxPixelData = new List<byte>();
            List<Hsv> newHsvValues = new List<Hsv>();

            var pixelCount = (int)(Math.Round(minDimension) * Math.Round(minDimension));
            var pixelDataSize = pixelCount * 4;
            bgraMinPixelData.Capacity = pixelDataSize;

            // We'll only save pixel data for the middle bitmaps if our third dimension is hue.
            if (channels == ColorSpectrumChannels.ValueSaturation ||
                channels == ColorSpectrumChannels.SaturationValue)
            {
                bgraMiddle1PixelData.Capacity = pixelDataSize;
                bgraMiddle2PixelData.Capacity = pixelDataSize;
                bgraMiddle3PixelData.Capacity = pixelDataSize;
                bgraMiddle4PixelData.Capacity = pixelDataSize;
            }

            bgraMaxPixelData.Capacity = pixelDataSize;
            newHsvValues.Capacity = pixelCount;

            int minDimensionInt = (int)Math.Round(minDimension);

            await Task.Run(() =>
            {
                // As the user perceives it, every time the third dimension not represented in the ColorSpectrum changes,
                // the ColorSpectrum will visually change to accommodate that value.  For example, if the ColorSpectrum handles hue and luminosity,
                // and the saturation externally goes from 1.0 to 0.5, then the ColorSpectrum will visually change to look more washed out
                // to represent that third dimension's new value.
                // Internally, however, we don't want to regenerate the ColorSpectrum bitmap every single time this happens, since that's very expensive.
                // In order to make it so that we don't have to, we implement an optimization where, rather than having only one bitmap,
                // we instead have multiple that we blend together using opacity to create the effect that we want.
                // In the case where the third dimension is saturation or luminosity, we only need two: one bitmap at the minimum value
                // of the third dimension, and one bitmap at the maximum.  Then we set the second's opacity at whatever the value of
                // the third dimension is - e.g., a saturation of 0.5 implies an opacity of 50%.
                // In the case where the third dimension is hue, we need six: one bitmap corresponding to red, yellow, green, cyan, blue, and purple.
                // We'll then blend between whichever colors our hue exists between - e.g., an orange color would use red and yellow with an opacity of 50%.
                // This optimization does incur slightly more startup time initially since we have to generate multiple bitmaps at once instead of only one,
                // but the running time savings after that are *huge* when we can just set an opacity instead of generating a brand new bitmap.
                if (shape == ColorSpectrumShape.Box)
                {
                    for (int x = minDimensionInt - 1; x >= 0; --x)
                    {
                        for (int y = minDimensionInt - 1; y >= 0; --y)
                        {
                            FillPixelForBox(
                                x, y, hsv, minDimensionInt, channels, minHue, maxHue, minSaturation, maxSaturation, minValue, maxValue,
                                bgraMinPixelData, bgraMiddle1PixelData, bgraMiddle2PixelData, bgraMiddle3PixelData, bgraMiddle4PixelData, bgraMaxPixelData,
                                newHsvValues);
                        }
                    }
                }
                else
                {
                    for (int y = 0; y < minDimensionInt; ++y)
                    {
                        for (int x = 0; x < minDimensionInt; ++x)
                        {
                            FillPixelForRing(
                                x, y, minDimensionInt / 2.0, hsv, channels, minHue, maxHue, minSaturation, maxSaturation, minValue, maxValue,
                                bgraMinPixelData, bgraMiddle1PixelData, bgraMiddle2PixelData, bgraMiddle3PixelData, bgraMiddle4PixelData, bgraMaxPixelData,
                                newHsvValues);
                        }
                    }
                }
            });

            Dispatcher.UIThread.Post(() =>
            {
                int pixelWidth = (int)Math.Round(minDimension);
                int pixelHeight = (int)Math.Round(minDimension);

                ColorSpectrumChannels channels2 = Channels;

                WriteableBitmap minBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMinPixelData);
                WriteableBitmap maxBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMaxPixelData);

                switch (channels2)
                {
                    case ColorSpectrumChannels.HueValue:
                    case ColorSpectrumChannels.ValueHue:
                        _saturationMinimumBitmap = minBitmap;
                        _saturationMaximumBitmap = maxBitmap;
                        break;
                    case ColorSpectrumChannels.HueSaturation:
                    case ColorSpectrumChannels.SaturationHue:
                        _valueBitmap = maxBitmap;
                        break;
                    case ColorSpectrumChannels.ValueSaturation:
                    case ColorSpectrumChannels.SaturationValue:
                        _hueRedBitmap = minBitmap;
                        _hueYellowBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle1PixelData);
                        _hueGreenBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle2PixelData);
                        _hueCyanBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle3PixelData);
                        _hueBlueBitmap = ColorHelpers.CreateBitmapFromPixelData(pixelWidth, pixelHeight, bgraMiddle4PixelData);
                        _huePurpleBitmap = maxBitmap;
                        break;
                }

                _shapeFromLastBitmapCreation = Shape;
                _componentsFromLastBitmapCreation = Channels;
                _imageWidthFromLastBitmapCreation = minDimension;
                _imageHeightFromLastBitmapCreation = minDimension;
                _minHueFromLastBitmapCreation = MinHue;
                _maxHueFromLastBitmapCreation = MaxHue;
                _minSaturationFromLastBitmapCreation = MinSaturation;
                _maxSaturationFromLastBitmapCreation = MaxSaturation;
                _minValueFromLastBitmapCreation = MinValue;
                _maxValueFromLastBitmapCreation = MaxValue;

                _hsvValues = newHsvValues;

                UpdateBitmapSources();
                UpdateEllipse();
            });
        }

        private void FillPixelForBox(
            double x,
            double y,
            Hsv baseHsv,
            double minDimension,
            ColorSpectrumChannels channels,
            double minHue,
            double maxHue,
            double minSaturation,
            double maxSaturation,
            double minValue,
            double maxValue,
            List<byte> bgraMinPixelData,
            List<byte> bgraMiddle1PixelData,
            List<byte> bgraMiddle2PixelData,
            List<byte> bgraMiddle3PixelData,
            List<byte> bgraMiddle4PixelData,
            List<byte> bgraMaxPixelData,
            List<Hsv> newHsvValues)
        {
            double hMin = minHue;
            double hMax = maxHue;
            double sMin = minSaturation / 100.0;
            double sMax = maxSaturation / 100.0;
            double vMin = minValue / 100.0;
            double vMax = maxValue / 100.0;

            Hsv hsvMin = baseHsv;
            Hsv hsvMiddle1 = baseHsv;
            Hsv hsvMiddle2 = baseHsv;
            Hsv hsvMiddle3 = baseHsv;
            Hsv hsvMiddle4 = baseHsv;
            Hsv hsvMax = baseHsv;

            double xPercent = (minDimension - 1 - x) / (minDimension - 1);
            double yPercent = (minDimension - 1 - y) / (minDimension - 1);

            switch (channels)
            {
                case ColorSpectrumChannels.HueValue:
                    hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + yPercent * (hMax - hMin);
                    hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + xPercent * (vMax - vMin);
                    hsvMin.S = 0;
                    hsvMax.S = 1;
                    break;

                case ColorSpectrumChannels.HueSaturation:
                    hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + yPercent * (hMax - hMin);
                    hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + xPercent * (sMax - sMin);
                    hsvMin.V = 0;
                    hsvMax.V = 1;
                    break;

                case ColorSpectrumChannels.ValueHue:
                    hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + yPercent * (vMax - vMin);
                    hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + xPercent * (hMax - hMin);
                    hsvMin.S = 0;
                    hsvMax.S = 1;
                    break;

                case ColorSpectrumChannels.ValueSaturation:
                    hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + yPercent * (vMax - vMin);
                    hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + xPercent * (sMax - sMin);
                    hsvMin.H = 0;
                    hsvMiddle1.H = 60;
                    hsvMiddle2.H = 120;
                    hsvMiddle3.H = 180;
                    hsvMiddle4.H = 240;
                    hsvMax.H = 300;
                    break;

                case ColorSpectrumChannels.SaturationHue:
                    hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + yPercent * (sMax - sMin);
                    hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + xPercent * (hMax - hMin);
                    hsvMin.V = 0;
                    hsvMax.V = 1;
                    break;

                case ColorSpectrumChannels.SaturationValue:
                    hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + yPercent * (sMax - sMin);
                    hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + xPercent * (vMax - vMin);
                    hsvMin.H = 0;
                    hsvMiddle1.H = 60;
                    hsvMiddle2.H = 120;
                    hsvMiddle3.H = 180;
                    hsvMiddle4.H = 240;
                    hsvMax.H = 300;
                    break;
            }

            // If saturation is an axis in the spectrum with hue, or value is an axis, then we want
            // that axis to go from maximum at the top to minimum at the bottom,
            // or maximum at the outside to minimum at the inside in the case of the ring configuration,
            // so we'll invert the number before assigning the HSL value to the array.
            // Otherwise, we'll have a very narrow section in the middle that actually has meaningful hue
            // in the case of the ring configuration.
            if (channels == ColorSpectrumChannels.HueSaturation ||
                channels == ColorSpectrumChannels.SaturationHue)
            {
                hsvMin.S = sMax - hsvMin.S + sMin;
                hsvMiddle1.S = sMax - hsvMiddle1.S + sMin;
                hsvMiddle2.S = sMax - hsvMiddle2.S + sMin;
                hsvMiddle3.S = sMax - hsvMiddle3.S + sMin;
                hsvMiddle4.S = sMax - hsvMiddle4.S + sMin;
                hsvMax.S = sMax - hsvMax.S + sMin;
            }
            else
            {
                hsvMin.V = vMax - hsvMin.V + vMin;
                hsvMiddle1.V = vMax - hsvMiddle1.V + vMin;
                hsvMiddle2.V = vMax - hsvMiddle2.V + vMin;
                hsvMiddle3.V = vMax - hsvMiddle3.V + vMin;
                hsvMiddle4.V = vMax - hsvMiddle4.V + vMin;
                hsvMax.V = vMax - hsvMax.V + vMin;
            }

            newHsvValues.Add(hsvMin);

            Rgb rgbMin = hsvMin.ToRgb();
            bgraMinPixelData.Add((byte)Math.Round(rgbMin.B * 255.0)); // b
            bgraMinPixelData.Add((byte)Math.Round(rgbMin.G * 255.0)); // g
            bgraMinPixelData.Add((byte)Math.Round(rgbMin.R * 255.0)); // r
            bgraMinPixelData.Add(255); // a - ignored

            // We'll only save pixel data for the middle bitmaps if our third dimension is hue.
            if (channels == ColorSpectrumChannels.ValueSaturation ||
                channels == ColorSpectrumChannels.SaturationValue)
            {
                Rgb rgbMiddle1 = hsvMiddle1.ToRgb();
                bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.B * 255.0)); // b
                bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.G * 255.0)); // g
                bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.R * 255.0)); // r
                bgraMiddle1PixelData.Add(255); // a - ignored

                Rgb rgbMiddle2 = hsvMiddle2.ToRgb();
                bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.B * 255.0)); // b
                bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.G * 255.0)); // g
                bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.R * 255.0)); // r
                bgraMiddle2PixelData.Add(255); // a - ignored

                Rgb rgbMiddle3 = hsvMiddle3.ToRgb();
                bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.B * 255.0)); // b
                bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.G * 255.0)); // g
                bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.R * 255.0)); // r
                bgraMiddle3PixelData.Add(255); // a - ignored

                Rgb rgbMiddle4 = hsvMiddle4.ToRgb();
                bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.B * 255.0)); // b
                bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.G * 255.0)); // g
                bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.R * 255.0)); // r
                bgraMiddle4PixelData.Add(255); // a - ignored
            }

            Rgb rgbMax = hsvMax.ToRgb();
            bgraMaxPixelData.Add((byte)Math.Round(rgbMax.B * 255.0)); // b
            bgraMaxPixelData.Add((byte)Math.Round(rgbMax.G * 255.0)); // g
            bgraMaxPixelData.Add((byte)Math.Round(rgbMax.R * 255.0)); // r
            bgraMaxPixelData.Add(255); // a - ignored
        }

        private void FillPixelForRing(
            double x,
            double y,
            double radius,
            Hsv baseHsv,
            ColorSpectrumChannels channels,
            double minHue,
            double maxHue,
            double minSaturation,
            double maxSaturation,
            double minValue,
            double maxValue,
            List<byte> bgraMinPixelData,
            List<byte> bgraMiddle1PixelData,
            List<byte> bgraMiddle2PixelData,
            List<byte> bgraMiddle3PixelData,
            List<byte> bgraMiddle4PixelData,
            List<byte> bgraMaxPixelData,
            List<Hsv> newHsvValues)
        {
            double hMin = minHue;
            double hMax = maxHue;
            double sMin = minSaturation / 100.0;
            double sMax = maxSaturation / 100.0;
            double vMin = minValue / 100.0;
            double vMax = maxValue / 100.0;

            double distanceFromRadius = Math.Sqrt(Math.Pow(x - radius, 2) + Math.Pow(y - radius, 2));

            double xToUse = x;
            double yToUse = y;

            // If we're outside the ring, then we want the pixel to appear as blank.
            // However, to avoid issues with rounding errors, we'll act as though this point
            // is on the edge of the ring for the purposes of returning an HSL value.
            // That way, hit testing on the edges will always return the correct value.
            if (distanceFromRadius > radius)
            {
                xToUse = (radius / distanceFromRadius) * (x - radius) + radius;
                yToUse = (radius / distanceFromRadius) * (y - radius) + radius;
                distanceFromRadius = radius;
            }

            Hsv hsvMin = baseHsv;
            Hsv hsvMiddle1 = baseHsv;
            Hsv hsvMiddle2 = baseHsv;
            Hsv hsvMiddle3 = baseHsv;
            Hsv hsvMiddle4 = baseHsv;
            Hsv hsvMax = baseHsv;

            double r = 1 - distanceFromRadius / radius;

            double theta = Math.Atan2((radius - yToUse), (radius - xToUse)) * 180.0 / Math.PI;
            theta += 180.0;
            theta = Math.Floor(theta);

            while (theta > 360)
            {
                theta -= 360;
            }

            double thetaPercent = theta / 360;

            switch (channels)
            {
                case ColorSpectrumChannels.HueValue:
                    hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + thetaPercent * (hMax - hMin);
                    hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + r * (vMax - vMin);
                    hsvMin.S = 0;
                    hsvMax.S = 1;
                    break;

                case ColorSpectrumChannels.HueSaturation:
                    hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + thetaPercent * (hMax - hMin);
                    hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + r * (sMax - sMin);
                    hsvMin.V = 0;
                    hsvMax.V = 1;
                    break;

                case ColorSpectrumChannels.ValueHue:
                    hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + thetaPercent * (vMax - vMin);
                    hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + r * (hMax - hMin);
                    hsvMin.S = 0;
                    hsvMax.S = 1;
                    break;

                case ColorSpectrumChannels.ValueSaturation:
                    hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + thetaPercent * (vMax - vMin);
                    hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + r * (sMax - sMin);
                    hsvMin.H = 0;
                    hsvMiddle1.H = 60;
                    hsvMiddle2.H = 120;
                    hsvMiddle3.H = 180;
                    hsvMiddle4.H = 240;
                    hsvMax.H = 300;
                    break;

                case ColorSpectrumChannels.SaturationHue:
                    hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + thetaPercent * (sMax - sMin);
                    hsvMin.H = hsvMiddle1.H = hsvMiddle2.H = hsvMiddle3.H = hsvMiddle4.H = hsvMax.H = hMin + r * (hMax - hMin);
                    hsvMin.V = 0;
                    hsvMax.V = 1;
                    break;

                case ColorSpectrumChannels.SaturationValue:
                    hsvMin.S = hsvMiddle1.S = hsvMiddle2.S = hsvMiddle3.S = hsvMiddle4.S = hsvMax.S = sMin + thetaPercent * (sMax - sMin);
                    hsvMin.V = hsvMiddle1.V = hsvMiddle2.V = hsvMiddle3.V = hsvMiddle4.V = hsvMax.V = vMin + r * (vMax - vMin);
                    hsvMin.H = 0;
                    hsvMiddle1.H = 60;
                    hsvMiddle2.H = 120;
                    hsvMiddle3.H = 180;
                    hsvMiddle4.H = 240;
                    hsvMax.H = 300;
                    break;
            }

            // If saturation is an axis in the spectrum with hue, or value is an axis, then we want
            // that axis to go from maximum at the top to minimum at the bottom,
            // or maximum at the outside to minimum at the inside in the case of the ring configuration,
            // so we'll invert the number before assigning the HSL value to the array.
            // Otherwise, we'll have a very narrow section in the middle that actually has meaningful hue
            // in the case of the ring configuration.
            if (channels == ColorSpectrumChannels.HueSaturation ||
                channels == ColorSpectrumChannels.SaturationHue)
            {
                hsvMin.S = sMax - hsvMin.S + sMin;
                hsvMiddle1.S = sMax - hsvMiddle1.S + sMin;
                hsvMiddle2.S = sMax - hsvMiddle2.S + sMin;
                hsvMiddle3.S = sMax - hsvMiddle3.S + sMin;
                hsvMiddle4.S = sMax - hsvMiddle4.S + sMin;
                hsvMax.S = sMax - hsvMax.S + sMin;
            }
            else
            {
                hsvMin.V = vMax - hsvMin.V + vMin;
                hsvMiddle1.V = vMax - hsvMiddle1.V + vMin;
                hsvMiddle2.V = vMax - hsvMiddle2.V + vMin;
                hsvMiddle3.V = vMax - hsvMiddle3.V + vMin;
                hsvMiddle4.V = vMax - hsvMiddle4.V + vMin;
                hsvMax.V = vMax - hsvMax.V + vMin;
            }

            newHsvValues.Add(hsvMin);

            Rgb rgbMin = hsvMin.ToRgb();
            bgraMinPixelData.Add((byte)Math.Round(rgbMin.B * 255)); // b
            bgraMinPixelData.Add((byte)Math.Round(rgbMin.G * 255)); // g
            bgraMinPixelData.Add((byte)Math.Round(rgbMin.R * 255)); // r
            bgraMinPixelData.Add(255); // a

            // We'll only save pixel data for the middle bitmaps if our third dimension is hue.
            if (channels == ColorSpectrumChannels.ValueSaturation ||
                channels == ColorSpectrumChannels.SaturationValue)
            {
                Rgb rgbMiddle1 = hsvMiddle1.ToRgb();
                bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.B * 255)); // b
                bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.G * 255)); // g
                bgraMiddle1PixelData.Add((byte)Math.Round(rgbMiddle1.R * 255)); // r
                bgraMiddle1PixelData.Add(255); // a

                Rgb rgbMiddle2 = hsvMiddle2.ToRgb();
                bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.B * 255)); // b
                bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.G * 255)); // g
                bgraMiddle2PixelData.Add((byte)Math.Round(rgbMiddle2.R * 255)); // r
                bgraMiddle2PixelData.Add(255); // a

                Rgb rgbMiddle3 = hsvMiddle3.ToRgb();
                bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.B * 255)); // b
                bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.G * 255)); // g
                bgraMiddle3PixelData.Add((byte)Math.Round(rgbMiddle3.R * 255)); // r
                bgraMiddle3PixelData.Add(255); // a

                Rgb rgbMiddle4 = hsvMiddle4.ToRgb();
                bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.B * 255)); // b
                bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.G * 255)); // g
                bgraMiddle4PixelData.Add((byte)Math.Round(rgbMiddle4.R * 255)); // r
                bgraMiddle4PixelData.Add(255); // a
            }

            Rgb rgbMax = hsvMax.ToRgb();
            bgraMaxPixelData.Add((byte)Math.Round(rgbMax.B * 255)); // b
            bgraMaxPixelData.Add((byte)Math.Round(rgbMax.G * 255)); // g
            bgraMaxPixelData.Add((byte)Math.Round(rgbMax.R * 255)); // r
            bgraMaxPixelData.Add(255); // a
        }

        private void UpdateBitmapSources()
        {
            if (_spectrumOverlayRectangle == null ||
                _spectrumOverlayEllipse == null ||
                _spectrumRectangle == null ||
                _spectrumEllipse == null)
            {
                return;
            }

            HsvColor hsvColor = HsvColor;
            ColorSpectrumChannels channels = Channels;

            // We'll set the base image and the overlay image based on which component is our third dimension.
            // If it's saturation or luminosity, then the base image is that dimension at its minimum value,
            // while the overlay image is that dimension at its maximum value.
            // If it's hue, then we'll figure out where in the color wheel we are, and then use the two
            // colors on either side of our position as our base image and overlay image.
            // For example, if our hue is orange, then the base image would be red and the overlay image yellow.
            switch (channels)
            {
                case ColorSpectrumChannels.HueValue:
                case ColorSpectrumChannels.ValueHue:
                    {
                        if (_saturationMinimumBitmap == null ||
                            _saturationMaximumBitmap == null)
                        {
                            return;
                        }

                        ImageBrush spectrumBrush = new ImageBrush(_saturationMinimumBitmap);
                        ImageBrush spectrumOverlayBrush = new ImageBrush(_saturationMaximumBitmap);

                        _spectrumOverlayRectangle.Opacity = hsvColor.S;
                        _spectrumOverlayEllipse.Opacity = hsvColor.S;
                        _spectrumRectangle.Fill = spectrumBrush;
                        _spectrumEllipse.Fill = spectrumBrush;
                        _spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
                        _spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
                    }
                    break;

                case ColorSpectrumChannels.HueSaturation:
                case ColorSpectrumChannels.SaturationHue:
                    {
                        if (_valueBitmap == null)
                        {
                            return;
                        }

                        ImageBrush spectrumBrush = new ImageBrush(_valueBitmap);
                        ImageBrush spectrumOverlayBrush = new ImageBrush(_valueBitmap);

                        _spectrumOverlayRectangle.Opacity = 1.0;
                        _spectrumOverlayEllipse.Opacity = 1.0;
                        _spectrumRectangle.Fill = spectrumBrush;
                        _spectrumEllipse.Fill = spectrumBrush;
                        _spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
                        _spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
                    }
                    break;

                case ColorSpectrumChannels.ValueSaturation:
                case ColorSpectrumChannels.SaturationValue:
                    {
                        if (_hueRedBitmap == null ||
                            _hueYellowBitmap == null ||
                            _hueGreenBitmap == null ||
                            _hueCyanBitmap == null ||
                            _hueBlueBitmap == null ||
                            _huePurpleBitmap == null)
                        {
                            return;
                        }

                        ImageBrush spectrumBrush;
                        ImageBrush spectrumOverlayBrush;

                        double sextant = hsvColor.H / 60.0;

                        if (sextant < 1)
                        {
                            spectrumBrush = new ImageBrush(_hueRedBitmap);
                            spectrumOverlayBrush = new ImageBrush(_hueYellowBitmap);
                        }
                        else if (sextant >= 1 && sextant < 2)
                        {
                            spectrumBrush = new ImageBrush(_hueYellowBitmap);
                            spectrumOverlayBrush = new ImageBrush(_hueGreenBitmap);
                        }
                        else if (sextant >= 2 && sextant < 3)
                        {
                            spectrumBrush = new ImageBrush(_hueGreenBitmap);
                            spectrumOverlayBrush = new ImageBrush(_hueCyanBitmap);
                        }
                        else if (sextant >= 3 && sextant < 4)
                        {
                            spectrumBrush = new ImageBrush(_hueCyanBitmap);
                            spectrumOverlayBrush = new ImageBrush(_hueBlueBitmap);
                        }
                        else if (sextant >= 4 && sextant < 5)
                        {
                            spectrumBrush = new ImageBrush(_hueBlueBitmap);
                            spectrumOverlayBrush = new ImageBrush(_huePurpleBitmap);
                        }
                        else
                        {
                            spectrumBrush = new ImageBrush(_huePurpleBitmap);
                            spectrumOverlayBrush = new ImageBrush(_hueRedBitmap);
                        }

                        _spectrumOverlayRectangle.Opacity = sextant - (int)sextant;
                        _spectrumOverlayEllipse.Opacity = sextant - (int)sextant;
                        _spectrumRectangle.Fill = spectrumBrush;
                        _spectrumEllipse.Fill = spectrumBrush;
                        _spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
                        _spectrumOverlayRectangle.Fill = spectrumOverlayBrush;
                    }
                    break;
            }
        }

        /// <summary>
        /// Determines whether the selection ellipse should be light based on the relative
        /// luminance of the selected color.
        /// </summary>
        private bool SelectionEllipseShouldBeLight()
        {
            // The selection ellipse should be light if and only if the chosen color
            // contrasts more with black than it does with white.
            // To find how much something contrasts with white, we use the equation
            // for relative luminance.
            //
            // If the third channel is value, then we won't be updating the spectrum's displayed colors,
            // so in that case we should use a value of 1 when considering the backdrop
            // for the selection ellipse.
            Color displayedColor;

            if (Channels == ColorSpectrumChannels.HueSaturation ||
                Channels == ColorSpectrumChannels.SaturationHue)
            {
                HsvColor hsvColor = HsvColor;
                Rgb color = (new Hsv(hsvColor.H, hsvColor.S, 1.0)).ToRgb();
                displayedColor = color.ToColor(hsvColor.A);
            }
            else
            {
                displayedColor = Color;
            }

            var lum = ColorHelpers.GetRelativeLuminance(displayedColor);

            return lum <= 0.5;
        }
    }
}
