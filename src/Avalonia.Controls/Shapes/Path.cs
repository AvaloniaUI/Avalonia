// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Path : Shape
    {
        public static readonly StyledProperty<Geometry> DataProperty =
            AvaloniaProperty.Register<Path, Geometry>(nameof(Data));

        static Path()
        {
            AffectsGeometry<Path>(DataProperty);
        }

        public Geometry Data
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        protected override Geometry CreateDefiningGeometry() => Data;
    }
}
