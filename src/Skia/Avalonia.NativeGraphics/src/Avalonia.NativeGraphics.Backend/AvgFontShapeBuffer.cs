using System;
using Avalonia.Native.Interop;

namespace Avalonia.NativeGraphics.Backend
{
    public class AvgFontShapeBuffer
    {
        private IAvgFontShapeBuffer _native;

        public AvgFontShapeBuffer(GlyphTypefaceImpl avgGlphTypeface)
        {
            _native = AvaloniaNativeGraphicsPlatform.Factory.CreateAvgFontShapeBuffer(avgGlphTypeface.Typeface);
        }

        public int Length => _native.Length;

        public void GuessSegmentProperties() => _native.GuessSegmentProperties();

        public void SetDirection(int direction) => _native.SetDirection(direction);

        unsafe public void SetLanguage(IntPtr language) => _native.SetLanguage((void*)language);

        unsafe public void AddUtf16(ReadOnlySpan<char> text)
        {
           AddUtf16(text, 0, text.Length);
        }

        unsafe public void AddUtf16(ReadOnlySpan<char> text, int itemOffset, int itemLength)
        {
            fixed (char* textptr = &text.GetPinnableReference())
            {
                _native.AddUtf16(textptr, text.Length, itemOffset, itemLength);
            }
        }

        public void Shape() => _native.Shape();

        unsafe public Span<AvgGlyphInfo> GetGlyphInfoSpan()
        {
            uint length = 0;
            return new Span<AvgGlyphInfo>(_native.GetGlyphInfoSpan(&length), (int) length);
        }

        unsafe public Span<AvgGlphPosition> GetGlyphPositionSpan()
        {
            uint length = 0;
            return new Span<AvgGlphPosition>(_native.GetGlyphPositionSpan(&length), (int) length);
        }

        unsafe public int GetScale()
        {
            int scale_x, scale_y;
            _native.GetScale(&scale_x, &scale_y);

            return scale_x;
        }


    }
}
