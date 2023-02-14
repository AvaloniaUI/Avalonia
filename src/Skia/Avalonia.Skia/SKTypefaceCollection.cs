using System.Collections.Concurrent;
using Avalonia.Media;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class SKTypefaceCollection
    {
        private readonly ConcurrentDictionary<Typeface, SKTypeface> _typefaces =
            new ConcurrentDictionary<Typeface, SKTypeface>();

        public void AddTypeface(Typeface key, SKTypeface typeface)
        {
            _typefaces.TryAdd(key, typeface);
        }

        public SKTypeface Get(Typeface typeface)
        {
            return GetNearestMatch(typeface);
        }

        private SKTypeface GetNearestMatch(Typeface key)
        {
            if (_typefaces.Count == 0)
            {
                return null;
            }
            
            if (_typefaces.TryGetValue(key, out var typeface))
            {
                return typeface;
            }

            if(key.Style != FontStyle.Normal)
            {
                key = new Typeface(key.FontFamily, FontStyle.Normal, key.Weight, key.Stretch);
            }

            if(key.Stretch != FontStretch.Normal)
            {
                if(TryFindStretchFallback(key, out typeface))
                {
                    return typeface;
                }
                
                if(key.Weight != FontWeight.Normal)
                {
                    if (TryFindStretchFallback(new Typeface(key.FontFamily, key.Style, FontWeight.Normal, key.Stretch), out typeface))
                    {
                        return typeface;
                    }
                }

                key = new Typeface(key.FontFamily, key.Style, key.Weight, FontStretch.Normal);
            }

            if(TryFindWeightFallback(key, out typeface))
            {
                return typeface;
            }

            if (TryFindStretchFallback(key, out typeface))
            {
                return typeface;
            }

            //Nothing was found so we try some regular typeface.
            if (_typefaces.TryGetValue(new Typeface(key.FontFamily), out typeface))
            {
                return typeface;
            }

            SKTypeface skTypeface = null;

            foreach(var pair in _typefaces)
            {
                skTypeface = pair.Value;

                if (skTypeface.FamilyName.Contains(key.FontFamily.Name))
                {
                    return skTypeface;
                }
            }

            return skTypeface;
        }

        private bool TryFindStretchFallback(Typeface key, out SKTypeface typeface)
        {
            typeface = null;
            var stretch = (int)key.Stretch;

            if (stretch < 5)
            {
                for (var i = 0; stretch + i < 9; i++)
                {
                    if (_typefaces.TryGetValue(new Typeface(key.FontFamily, key.Style, key.Weight, (FontStretch)(stretch + i)), out typeface))
                    {
                        return true;
                    }
                }
            }
            else
            {
                for (var i = 0; stretch - i > 1; i++)
                {
                    if (_typefaces.TryGetValue(new Typeface(key.FontFamily, key.Style, key.Weight, (FontStretch)(stretch - i)), out typeface))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryFindWeightFallback(Typeface key, out SKTypeface typeface)
        {
            typeface = null;
            var weight = (int)key.Weight;

            //If the target weight given is between 400 and 500 inclusive          
            if (weight >= 400 && weight <= 500)
            {
                //Look for available weights between the target and 500, in ascending order.
                for (var i = 0; weight + i <= 500; i += 50)
                {
                    if (_typefaces.TryGetValue(new Typeface(key.FontFamily, key.Style, (FontWeight)(weight + i), key.Stretch), out typeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (_typefaces.TryGetValue(new Typeface(key.FontFamily, key.Style, (FontWeight)(weight - i), key.Stretch), out typeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights greater than 500, in ascending order.
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (_typefaces.TryGetValue(new Typeface(key.FontFamily, key.Style, (FontWeight)(weight + i), key.Stretch), out typeface))
                    {
                        return true;
                    }
                }
            }

            //If a weight less than 400 is given, look for available weights less than the target, in descending order.           
            if (weight < 400)
            {
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (_typefaces.TryGetValue(new Typeface(key.FontFamily, key.Style, (FontWeight)(weight - i), key.Stretch), out typeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (_typefaces.TryGetValue(new Typeface(key.FontFamily, key.Style, (FontWeight)(weight + i), key.Stretch), out typeface))
                    {
                        return true;
                    }
                }
            }

            //If a weight greater than 500 is given, look for available weights greater than the target, in ascending order.
            if (weight > 500)
            {
                for (var i = 0; weight + i <= 900; i += 50)
                {
                    if (_typefaces.TryGetValue(new Typeface(key.FontFamily, key.Style, (FontWeight)(weight + i), key.Stretch), out typeface))
                    {
                        return true;
                    }
                }

                //If no match is found, look for available weights less than the target, in descending order.
                for (var i = 0; weight - i >= 100; i += 50)
                {
                    if (_typefaces.TryGetValue(new Typeface(key.FontFamily, key.Style, (FontWeight)(weight - i), key.Stretch), out typeface))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
