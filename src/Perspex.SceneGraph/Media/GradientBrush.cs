// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Perspex.Media
{
    public abstract class GradientBrush : Brush
    {
        public static readonly PerspexProperty<BrushMappingMode> MappingModeProperty =
PerspexProperty.Register<GradientBrush, BrushMappingMode>(nameof(MappingMode), BrushMappingMode.RelativeToBoundingBox);

        public static readonly PerspexProperty<GradientSpreadMethod> SpreadMethodProperty =
PerspexProperty.Register<GradientBrush, GradientSpreadMethod>(nameof(SpreadMethod), GradientSpreadMethod.Pad);

        public static readonly PerspexProperty<List<GradientStop>> GradientStopsProperty =
PerspexProperty.Register<GradientBrush, List<GradientStop>>(nameof(Opacity), new List<GradientStop>());

        public GradientBrush()
        {
        }

        public BrushMappingMode MappingMode
        {
            get { return GetValue(MappingModeProperty); }
            set { SetValue(MappingModeProperty, value); }
        }

        public GradientSpreadMethod SpreadMethod
        {
            get { return GetValue(SpreadMethodProperty); }
            set { SetValue(SpreadMethodProperty, value); }
        }

        public List<GradientStop> GradientStops
        {
            get { return GetValue(GradientStopsProperty); }
            set { SetValue(GradientStopsProperty, value); }
        }
    }
}