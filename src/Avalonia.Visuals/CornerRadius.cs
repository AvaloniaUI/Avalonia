// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Animation.Animators;
using Avalonia.Utilities;

namespace Avalonia
{
    public struct CornerRadius
    {
        static CornerRadius()
        {
            Animation.Animation.RegisterAnimator<CornerRadiusAnimator>(prop => typeof(CornerRadius).IsAssignableFrom(prop.PropertyType));
        }

        public CornerRadius(double uniformRadius)
        {
            TopLeft = TopRight = BottomLeft = BottomRight = uniformRadius;

        }
        public CornerRadius(double top, double bottom)
        {
            TopLeft = TopRight = top;
            BottomLeft = BottomRight = bottom;
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
        public bool IsEmpty => TopLeft.Equals(0) && IsUniform;
        public bool IsUniform => TopLeft.Equals(TopRight) && BottomLeft.Equals(BottomRight) && TopRight.Equals(BottomRight);

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

        public static CornerRadius Parse(string s)
        {
            using (var tokenizer = new StringTokenizer(s, CultureInfo.InvariantCulture, exceptionMessage: "Invalid Thickness"))
            {
                if (tokenizer.TryReadDouble(out var a))
                {
                    if (tokenizer.TryReadDouble(out var b))
                    {
                        if (tokenizer.TryReadDouble(out var c))
                        {
                            return new CornerRadius(a, b, c, tokenizer.ReadDouble());
                        }

                        return new CornerRadius(a, b);
                    }

                    return new CornerRadius(a);
                }

                throw new FormatException("Invalid CornerRadius.");
            }
        }

        public static bool operator ==(CornerRadius cr1, CornerRadius cr2)
        {
            return cr1.TopLeft.Equals(cr2.TopLeft)
                   && cr1.TopRight.Equals(cr2.TopRight)
                   && cr1.BottomRight.Equals(cr2.BottomRight)
                   && cr1.BottomLeft.Equals(cr2.BottomLeft);
        }

        public static bool operator !=(CornerRadius cr1, CornerRadius cr2)
        {
            return !(cr1 == cr2);
        }
    }
}
