#include "AvgGlyphRun.h"
#include "AvgFontManager.h"
#include "AvgGlyphTypeface.h"

AvgGlyphRun::AvgGlyphRun(IAvgFontManager* fontManager, IAvgGlyphTypeface* typeface)
{
    auto avgFontManager = dynamic_cast<AvgFontManager*>(fontManager);
    auto avgTypeface = dynamic_cast<AvgGlyphTypeface*>(typeface);

    _fontManager = avgFontManager;

    avgFontManager->_font.setSubpixel(true);
    avgFontManager->_font.setEdging(SkFont::Edging::kSubpixelAntiAlias);
    avgFontManager->_font.setHinting(SkFontHinting::kFull);
    avgFontManager->_font.setLinearMetrics(true);

    avgFontManager->_font.setTypeface(avgTypeface->_typeface);
}

AvgGlyphRun::~AvgGlyphRun()
{
    // printf("Killing glyphrun\n");
}

void AvgGlyphRun::SetFontSize(float size)
{
    _fontManager->_font.setSize(size);
}

HRESULT AvgGlyphRun::AllocRun(int count)
{
    // _fontManager->_font.setSize(48.0);
    _runBuffer = _fontManager->_textBlobBuilder.allocRun(_fontManager->_font, count, 0, 0);
    // printf("Allocated buffer: %d\n", count);
    return 0;
}

HRESULT AvgGlyphRun::AllocHorizontalRun(int count)
{
    // _fontManager->_font.setSize(12.0);
    _runBuffer = _fontManager->_textBlobBuilder.allocRunPosH(_fontManager->_font, count, 0);
    return 0;
}

HRESULT AvgGlyphRun::AllocPositionedRun(int count)
{
    _runBuffer = _fontManager->_textBlobBuilder.allocRunPos(_fontManager->_font, count);
    return 0;
}

void* AvgGlyphRun::GetGlyphBuffer()
{
    // printf("Get the glyph buffer: %p\n", _runBuffer.glyphs);
    return _runBuffer.glyphs;
}

void* AvgGlyphRun::GetPositionsBuffer()
{
    // printf("Get the positions buffer()\n");
    return _runBuffer.pos;
}

void AvgGlyphRun::BuildText()
{
    _textBlob = _fontManager->_textBlobBuilder.make();
    // printf("Built the text blob!\n");
}
