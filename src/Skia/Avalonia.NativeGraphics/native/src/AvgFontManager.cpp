#include "AvgFontManager.h"
#include "AvgString.h"
#include "AvgGlyphTypeface.h"

#include <include/core/SkTypeface.h>

#include <stdio.h>

AvgFontManager::AvgFontManager()
{
    _fontMgr = SkFontMgr::RefDefault();

}

AvgFontManager::~AvgFontManager()
{
}

IAvgString* AvgFontManager::GetDefaultFamilyName()
{

    auto def = SkTypeface::MakeDefault();
    SkString s;
    def->getFamilyName(&s);
    auto string = new AvgString(s.c_str());
    return string;
}

int AvgFontManager::GetFontFamilyCount()
{
    return _fontMgr->countFamilies();
}

IAvgString* AvgFontManager::GetFamilyName(int index)
{
    SkString s;
    _fontMgr->getFamilyName(index, &s);

    auto string = new AvgString(s.c_str());
    return string;
}

IAvgGlyphTypeface* AvgFontManager::CreateGlyphTypeface(char* fontFamily, AvgTypeface typeface)
{
    SkTypeface* skTypeface = NULL;

    SkFontStyle::Slant slant;

    switch (typeface.FontStyle)
    {
    case Italic:
        slant = SkFontStyle::Slant::kItalic_Slant;
        break;
    case Oblique:
        slant = SkFontStyle::Slant::kOblique_Slant;
        break;
    case Normal:
    default:
        slant = SkFontStyle::Slant::kUpright_Slant;
        break;
    }

    auto fontstyle = SkFontStyle(typeface.FontWeight, typeface.FontStretch, slant);

    auto def = SkTypeface::MakeDefault();
    SkString defFamilyName;
    def->getFamilyName(&defFamilyName);

    skTypeface = _fontMgr->matchFamilyStyle(fontFamily, fontstyle);
    if (!skTypeface)
    {
        // printf("Couldn't match get default\n");
        // No matching typeface, then fallback the default.
        skTypeface = def.get();
    }

    int isFakeBold = (int)(typeface.FontWeight >= 600 && !skTypeface->isBold());
    int isFakeItalic = (int)(typeface.FontStyle == AvgFontStyle::Italic && !skTypeface->isItalic());

    return new AvgGlyphTypeface(skTypeface, isFakeBold, isFakeItalic);
}
