// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;

namespace Avalonia
{
    public struct CornerRadius
    {
        public CornerRadius(double uniformRadius)
        {
            TopLeft = TopRight = BottomLeft = BottomRight = uniformRadius;

        }
        public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;
        }

        public double TopLeft { get; }
        public double TopRight { get; }
        public double BottomRight { get; }
        public double BottomLeft { get; }
        public bool IsEmpty => TopLeft.Equals(0) && TopRight.Equals(0) && BottomRight.Equals(0) && BottomLeft.Equals(0);

        public override bool Equals(object obj)
        {
            if (obj is CornerRadius)
            {
                return this == (CornerRadius)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return TopLeft.GetHashCode() ^ TopRight.GetHashCode() ^ BottomLeft.GetHashCode() ^ BottomRight.GetHashCode();
        }

        public override string ToString()
        {
            return $"{TopLeft},{TopRight},{BottomRight},{BottomLeft}";
        }

        public static CornerRadius Parse(string s, CultureInfo culture)
        {
            var parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            switch (parts.Count)
            {
                case 1:
                    var uniform = double.Parse(parts[0], culture);
                    return new CornerRadius(uniform);
                case 4:
                    var topLeft = double.Parse(parts[0], culture);
                    var topRight = double.Parse(parts[1], culture);
                    var bottomRight = double.Parse(parts[2], culture);
                    var bottomLeft = double.Parse(parts[3], culture);
                    return new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
                default:
                    {
                        throw new FormatException("Invalid CornerRadius.");
                    }
            }
        }

        public static bool operator ==(CornerRadius cr1, CornerRadius cr2)
        {
            return ((cr1.TopLeft.Equals(cr2.TopLeft) || double.IsNaN(cr1.TopLeft) && double.IsNaN(cr2.TopLeft)))
                   && (cr1.TopRight.Equals(cr2.TopRight) || (double.IsNaN(cr1.TopRight) && double.IsNaN(cr2.TopRight)))
                   && (cr1.BottomRight.Equals(cr2.BottomRight) || double.IsNaN(cr1.BottomRight) && double.IsNaN(cr2.BottomRight))
                   && (cr1.BottomLeft.Equals(cr2.BottomLeft) || double.IsNaN(cr1.BottomLeft) && double.IsNaN(cr2.BottomLeft));
        }

        public static bool operator !=(CornerRadius cr1, CornerRadius cr2)
        {
            return (!(cr1 == cr2));
        }
    }
}