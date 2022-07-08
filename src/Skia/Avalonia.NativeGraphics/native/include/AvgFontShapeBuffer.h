#pragma once

#include "comimpl.h"
#include "interop.h"
#include <hb.h>

#include "AvgGlyphTypeface.h"

class AvgFontShapeBuffer : public ComSingleObject<IAvgFontShapeBuffer, &IID_IAvgFontShapeBuffer>
{
    ComPtr<IUnknown> _release;
    hb_buffer_t* _buffer;
    AvgGlyphTypeface* _typeface;

public:
    AvgFontShapeBuffer(IAvgGlyphTypeface* typeface);
    ~AvgFontShapeBuffer();
    FORWARD_IUNKNOWN();
    int GetLength() override;
    void GuessSegmentProperties() override;
    void SetDirection(int direction) override;
    void SetLanguage(void* language) override;
    void AddUtf16(void* utf16, int length, int itemOffset, int itemLength) override;
    void Shape() override;
    void GetScale(int* x, int* y) override;
    void* GetGlyphInfoSpan(unsigned int* length) override;
    void* GetGlyphPositionSpan(unsigned int* length) override;
};
