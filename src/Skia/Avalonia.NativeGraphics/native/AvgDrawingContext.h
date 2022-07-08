//
// Created by kekekeks on 04.05.22.
//

#ifndef AVALONIA_SKIA_AVGDRAWINGCONTEXT_H
#define AVALONIA_SKIA_AVGDRAWINGCONTEXT_H
#include "interop.h"
#include "comimpl.h"
#include "include/core/SkSurface.h"
#include "include/core/SkCanvas.h"


class AvgDrawingContext : public ComSingleObject<IAvgDrawingContext, &IID_IAvgDrawingContext> {
    sk_sp<SkSurface> _surface;
    SkCanvas* _canvas;
    ComPtr<IUnknown> _release;
public:
    AvgDrawingContext(sk_sp<SkSurface> surface, SkCanvas* canvas, IUnknown* release) :
        _surface(surface), _canvas(canvas), _release(release) {};
    FORWARD_IUNKNOWN()

    void SetTransform(AvgMatrix3x2 *matrix) override;

    void Clear(unsigned int color) override;

    void FillRect(AvgRect rect, unsigned int color) override;
    ~AvgDrawingContext() override
    {
        _canvas->flush();
    };
};


#endif //AVALONIA_SKIA_AVGDRAWINGCONTEXT_H
