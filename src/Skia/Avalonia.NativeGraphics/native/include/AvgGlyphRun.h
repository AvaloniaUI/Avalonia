#ifndef AVALONIA_SKIA_AVGGLYPHRUN_H
#define AVALONIA_SKIA_AVGGLYPHRUN_H

#include "AvgFontManager.h"
#include "comimpl.h"
#include "interop.h"

#include "include/core/SkFont.h"
#include "include/core/SkTextBlob.h"

class AvgGlyphRun : public ComSingleObject<IAvgGlyphRun, &IID_IAvgGlyphRun>
{
    ComPtr<IUnknown> _release;
    // SkFont _font;
    // SkTextBlobBuilder _textBlobBuilder;
    SkTextBlobBuilder::RunBuffer _runBuffer;


  public:
    FORWARD_IUNKNOWN();
    AvgFontManager* _fontManager;
    sk_sp<SkTextBlob> _textBlob;
    AvgGlyphRun(IAvgFontManager* fontManager, IAvgGlyphTypeface* typeface);
    ~AvgGlyphRun();

    void SetFontSize(float) override;

    HRESULT AllocRun(int count) override;
    HRESULT AllocHorizontalRun(int count) override;
    HRESULT AllocPositionedRun(int count) override;
    void* GetGlyphBuffer() override;
    void* GetPositionsBuffer() override;
    void BuildText() override;
};

#endif
