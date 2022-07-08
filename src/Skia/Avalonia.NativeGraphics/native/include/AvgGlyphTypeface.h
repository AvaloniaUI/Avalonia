#ifndef AVALONIA_SKIA_AVGGLYPHTYPEFACE_H
#define AVALONIA_SKIA_AVGGLYPHTYPEFACE_H

#include "comimpl.h"
#include "include/core/SkTypeface.h"
#include "interop.h"
#include <hb.h>

class AvgGlyphTypeface;

class AvgGlyphTypeface : public ComSingleObject<IAvgGlyphTypeface, &IID_IAvgGlyphTypeface>
{
    ComPtr<IUnknown> _release;

    hb_face_t* _face;

    int _ascent;
    int _descent;
    int _lineGap;
    int _underlinePosition;
    int _underlineThickness;
    int _strikethroughPosition;
    int _strikethroughThickness;

  public:
    FORWARD_IUNKNOWN();
    hb_font_t* _font;
    sk_sp<SkTypeface> _typeface;
    AvgGlyphTypeface(SkTypeface* typeFace, bool isFakeBold, bool isFakeItalic);
    ~AvgGlyphTypeface();

    hb_blob_t* GetTable(hb_face_t* face, hb_tag_t tag);

    unsigned int GetGlyph(unsigned int glyph) override;
    int GetGlyphAdvance(unsigned int glyph) override;
    int GetDesignEmHeight() override;
    int GetAscent() override;
    int GetDescent() override;
    int GetLineGap() override;
    int GetUnderlinePosition() override;
    int GetUnderlineThickness() override;
    int GetStrikethroughPosition() override;
    int GetStrikethroughThickness() override;
    int GetIsFixedPitch() override;
    int GetIsFakeBold() override;
    int GetIsFakeItalic() override;
};

#endif
