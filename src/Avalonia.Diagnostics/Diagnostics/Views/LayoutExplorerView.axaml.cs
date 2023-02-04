using System;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Diagnostics.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Avalonia.Reactive;

namespace Avalonia.Diagnostics.Views
{
    internal class LayoutExplorerView : UserControl
    {
        private readonly ThicknessEditor _borderArea;
        private readonly ThicknessEditor _paddingArea;
        private readonly Rectangle _horizontalSizeBegin;
        private readonly Rectangle _horizontalSizeEnd;
        private readonly Rectangle _verticalSizeBegin;
        private readonly Rectangle _verticalSizeEnd;
        private readonly Grid _layoutRoot;
        private readonly Border _horizontalSize;
        private readonly Border _verticalSize;
        private readonly Border _contentArea;

        public LayoutExplorerView()
        {
            InitializeComponent();

            _borderArea = this.GetControl<ThicknessEditor>("BorderArea");
            _paddingArea = this.GetControl<ThicknessEditor>("PaddingArea");

            _horizontalSizeBegin = this.GetControl<Rectangle>("HorizontalSizeBegin");
            _horizontalSizeEnd = this.GetControl<Rectangle>("HorizontalSizeEnd");
            _verticalSizeBegin = this.GetControl<Rectangle>("VerticalSizeBegin");
            _verticalSizeEnd = this.GetControl<Rectangle>("VerticalSizeEnd");

            _horizontalSize = this.GetControl<Border>("HorizontalSize");
            _verticalSize = this.GetControl<Border>("VerticalSize");

            _contentArea = this.GetControl<Border>("ContentArea");

            _layoutRoot = this.GetControl<Grid>("LayoutRoot");

            Visual? visual = _contentArea;
            while (visual != null && !ReferenceEquals(visual, this))
            {
                visual.GetPropertyChangedObservable(BoundsProperty)
                    .Subscribe(UpdateSizeGuidelines);
                visual = visual.VisualParent;
            }
            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void UpdateSizeGuidelines(AvaloniaPropertyChangedEventArgs _)
        {
            void UpdateGuidelines(Visual area)
            {
                // That's what TransformedBounds.Bounds actually was.
                // The code below doesn't really make sense to me, so I've just changed v.TransformedBounds.Bounds
                // to GetPseudoTransformedBounds
                Rect GetPseudoTransformedBounds(Visual v) => new(v.Bounds.Size);
                var bounds = GetPseudoTransformedBounds(area);
                
                {
                    // Horizontal guideline
                    {
                        var sizeArea = TranslateToRoot(GetPseudoTransformedBounds(_horizontalSize).BottomLeft,
                            _horizontalSize);

                        var start = TranslateToRoot(bounds.BottomLeft, area);

                        SetPosition(_horizontalSizeBegin, start);

                        var end = TranslateToRoot(bounds.BottomRight, area);

                        SetPosition(_horizontalSizeEnd, end.WithX(end.X - 1));

                        var height = sizeArea.Y - start.Y + 2;

                        _horizontalSizeBegin.Height = height;
                        _horizontalSizeEnd.Height = height;
                    }

                    // Vertical guideline
                    {
                        var sizeArea = TranslateToRoot(GetPseudoTransformedBounds(_verticalSize).TopRight, _verticalSize);

                        var start = TranslateToRoot(bounds.TopRight, area);

                        SetPosition(_verticalSizeBegin, start);

                        var end = TranslateToRoot(bounds.BottomRight, area);

                        SetPosition(_verticalSizeEnd, end.WithY(end.Y - 1));

                        var width = sizeArea.X - start.X + 2;

                        _verticalSizeBegin.Width = width;
                        _verticalSizeEnd.Width = width;
                    }
                }
            }

            Point TranslateToRoot(Point point, Visual from)
            {
                return from.TranslatePoint(point, _layoutRoot) ?? default;
            }

            static void SetPosition(Rectangle rect, Point start)
            {
                Canvas.SetLeft(rect, start.X);
                Canvas.SetTop(rect, start.Y);
            }

            if (_borderArea.IsPresent)
            {
                UpdateGuidelines(_borderArea);
            }
            else if (_paddingArea.IsPresent)
            {
                UpdateGuidelines(_paddingArea);
            }
            else
            {
                UpdateGuidelines(_contentArea);
            }
        }
    }
}
