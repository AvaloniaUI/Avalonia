#ifndef AVALONIA_SKIA_AVGFONTMANAGER_H
#define AVALONIA_SKIA_AVGFONTMANAGER_H

#include "comimpl.h"
#include "interop.h"
#include <include/core/SkFontMgr.h>
#include <include/core/SkFont.h>
#include <include/core/SkTextBlob.h>

class AvgFontManager : public ComSingleObject<IAvgFontManager, &IID_IAvgFontManager>
{
    ComPtr<IUnknown> _release;
    sk_sp< SkFontMgr > _fontMgr;
public:
    SkFont _font;
    SkTextBlobBuilder _textBlobBuilder;
    FORWARD_IUNKNOWN()
    AvgFontManager();
    ~AvgFontManager();

    IAvgString* GetDefaultFamilyName() override;
    int GetFontFamilyCount() override;
    IAvgString* GetFamilyName(int index) override;
    IAvgGlyphTypeface* CreateGlyphTypeface(char* fontFamily, AvgTypeface typeface) override;
};

#endif
