using System;
using Avalonia.Media;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.NativeGraphics.Backend
{
    internal class GlyphRunImpl : IGlyphRunImpl
    {
        private IAvgGlyphRun _avgGlyphRun;
        private int _count = 0;
        public GlyphRunImpl(IAvgGlyphRun avgGlyphRun)
        {
            _avgGlyphRun = avgGlyphRun;
        }

        public IAvgGlyphRun AvgGlyphRun => _avgGlyphRun;

        public void AllocRun(int count)
        {
            _avgGlyphRun.AllocRun(count);
            _count = count;
        }

        public void AllocHorizontalRun(int count)
        {
            _avgGlyphRun.AllocHorizontalRun(count);
            _count = count;
        }

        public void AllocPositionedRun(int count)
        {
            _avgGlyphRun.AllocPositionedRun(count);
            _count = count;
        }

        public void SetFontSize(float size)
        {
            _avgGlyphRun.SetFontSize(size);
        }

        public unsafe Span<ushort> GetGlyphSpan()
        {
            return new Span<ushort>(_avgGlyphRun.GlyphBuffer, _count);
        }

        public unsafe Span<float> GetPositionsSpan()
        {
            return new Span<float>(_avgGlyphRun.PositionsBuffer, _count);
        }

        public unsafe Span<AvgSkPosition> GetPositionsVectorSpan()
        {
            return new Span<AvgSkPosition>(_avgGlyphRun.PositionsBuffer, _count);
        }

        public void BuildText()
        {
            _avgGlyphRun.BuildText();
        }
        public void Dispose()
        {
            
        }
    }
}