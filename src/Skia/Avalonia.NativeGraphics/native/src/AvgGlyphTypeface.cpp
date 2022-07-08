#include "AvgGlyphTypeface.h"
#include <hb-ot.h>
#include <include/core/SkFont.h>
#include <include/core/SkFontMetrics.h>
#include <stdio.h>

struct hb_table_data
{
    AvgGlyphTypeface* instance;
    int size;
    void* data;
};

static hb_blob_t* hb_get_table(hb_face_t* face, hb_tag_t tag, void* userData)
{
    AvgGlyphTypeface* instance = (AvgGlyphTypeface*)userData;
    return instance->GetTable(face, tag);
}

static void hb_destroy_table(void* userData)
{
    struct hb_table_data* tbl = (struct hb_table_data*)userData;
    // printf("Freeing\n");
    if(tbl->data) {
        free(tbl->data);
    }
    free(tbl);
}

hb_blob_t* AvgGlyphTypeface::GetTable(hb_face_t* face, hb_tag_t tag)
{
    struct hb_table_data* tbl = (struct hb_table_data*)malloc(sizeof(struct hb_table_data));
    memset(tbl, 0, sizeof(*tbl));
    tbl->instance = this;
    tbl->size = _typeface->getTableSize(tag);
    if(tbl->size) {
        tbl->data = malloc(tbl->size);
    }

    _typeface->getTableData(tag, 0, tbl->size, tbl->data);

    return hb_blob_create((const char*)tbl->data, tbl->size, HB_MEMORY_MODE_READONLY, tbl, hb_destroy_table);
}

AvgGlyphTypeface::AvgGlyphTypeface(SkTypeface* typeface, bool isFakeBold, bool isFakeItalic) : _typeface(typeface)
{
    typeface->ref();
    _face = hb_face_create_for_tables(hb_get_table, this, NULL);
    hb_face_set_upem(_face, _typeface->getUnitsPerEm());

    _font = hb_font_create(_face);
    hb_ot_font_set_funcs(_font);

    SkFont f = SkFont(_typeface);

    SkFontMetrics metrics;
    f.getMetrics(&metrics);

    const double defaultFontRenderingEmSize = 12.0;
    double unitsPerEm = (double) typeface->getUnitsPerEm();

    _ascent = (int) ((metrics.fAscent / defaultFontRenderingEmSize) * unitsPerEm);
    // printf("Ascent is: %d\n", _ascent);
    _descent = (int) ((metrics.fDescent / defaultFontRenderingEmSize) * unitsPerEm);
    // printf("Descent is: %d\n", _descent);
    _lineGap = (int) ((metrics.fLeading / defaultFontRenderingEmSize) * unitsPerEm);
    _underlinePosition = ((metrics.fUnderlinePosition / defaultFontRenderingEmSize) * unitsPerEm);
    _underlineThickness = ((metrics.fUnderlineThickness / defaultFontRenderingEmSize) * unitsPerEm);

}

AvgGlyphTypeface::~AvgGlyphTypeface()
{
}

unsigned int AvgGlyphTypeface::GetGlyph(unsigned int codepoint)
{
    hb_codepoint_t glyph = 0;
    if(hb_font_get_glyph(_font, codepoint, 0, &glyph))
    {
        return glyph;
    }
    return 0;
}

int AvgGlyphTypeface::GetGlyphAdvance(unsigned int glyph)
{
    hb_position_t a = hb_font_get_glyph_h_advance(_font, glyph);

    // printf("Getting Glyph advance: %d\n", a);
    return a;
}

int AvgGlyphTypeface::GetDesignEmHeight()
{
    int units = _typeface->getUnitsPerEm();
    return units;
}

int AvgGlyphTypeface::GetAscent()
{
    return _ascent;
}

int AvgGlyphTypeface::GetDescent()
{
    return _descent;
}

int AvgGlyphTypeface::GetLineGap()
{
    return _lineGap;
}

int AvgGlyphTypeface::GetUnderlinePosition()
{
    return _underlinePosition;
}

int AvgGlyphTypeface::GetUnderlineThickness()
{
    return _underlineThickness;
}

int AvgGlyphTypeface::GetStrikethroughPosition()
{
    // printf("%s\n", __func__);
    return 0;
}

int AvgGlyphTypeface::GetStrikethroughThickness()
{
    // printf("%s\n", __func__);
    return 0;
}

int AvgGlyphTypeface::GetIsFixedPitch()
{
    return (int) _typeface->isFixedPitch();
}

int AvgGlyphTypeface::GetIsFakeBold()
{
    // printf("%s\n", __func__);
    return 0;
}

int AvgGlyphTypeface::GetIsFakeItalic()
{
    // printf("%s\n", __func__);
    return 0;
}
