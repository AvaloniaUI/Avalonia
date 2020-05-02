using System;
using Avalonia.Animation.Animators;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    public struct BoxShadow
    {
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double Blur { get; set; }
        public double Spread { get; set; }
        public Color Color { get; set; }

        static BoxShadow()
        {
            Animation.Animation.RegisterAnimator<BoxShadowAnimator>(prop =>
                typeof(BoxShadow).IsAssignableFrom(prop.PropertyType));
        }
        
        public bool Equals(BoxShadow other)
        {
            return OffsetX.Equals(other.OffsetX) && OffsetY.Equals(other.OffsetY) && Blur.Equals(other.Blur) && Spread.Equals(other.Spread) && Color.Equals(other.Color);
        }

        public override bool Equals(object obj)
        {
            return obj is BoxShadow other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OffsetX.GetHashCode();
                hashCode = (hashCode * 397) ^ OffsetY.GetHashCode();
                hashCode = (hashCode * 397) ^ Blur.GetHashCode();
                hashCode = (hashCode * 397) ^ Spread.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                return hashCode;
            }
        }

        public bool IsEmpty => OffsetX == 0 && OffsetY == 0 && Blur == 0 && Spread == 0;

        public static unsafe BoxShadow Parse(string s)
        {
            if (s == "none")
                return default;
            
            var separatorCount = 0;
            var separators = stackalloc char[4];
            for(var c = 0; c<s.Length; c++)
                if (s[c] == ',' || s[c] == ' ')
                {
                    if (separatorCount == 4)
                        throw new FormatException("Invalid box-shadow format");
                    separators[separatorCount] = s[c];
                    separatorCount++;
                }

            if (separatorCount != 2 && separatorCount > 4)
                throw new FormatException("Invalid box-shadow format");

            var tokenizer = new StringTokenizer(s);
            var offsetX = tokenizer.ReadDouble(separators[0]);
            var offsetY = tokenizer.ReadDouble(separators[1]);
            double blur = 0;
            double spread = 0;
            if (separatorCount > 2)
                blur = tokenizer.ReadDouble(separators[2]);
            if (separatorCount > 3)
                spread = tokenizer.ReadDouble(separators[3]);
            var color = Media.Color.Parse(tokenizer.ReadString());
            return new BoxShadow
            {
                OffsetX = offsetX,
                OffsetY = offsetY,
                Blur = blur,
                Spread = spread,
                Color = color
            };
        }

        public Rect TransformBounds(in Rect rect) 
            => rect.Translate(new Vector(OffsetX, OffsetY)).Inflate(Spread + Blur);
    }
}
