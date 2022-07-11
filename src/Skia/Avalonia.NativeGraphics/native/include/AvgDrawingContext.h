//
// Created by kekekeks on 04.05.22.
//

#ifndef AVALONIA_SKIA_AVGDRAWINGCONTEXT_H
#define AVALONIA_SKIA_AVGDRAWINGCONTEXT_H
#include "comimpl.h"
#include "include/core/SkCanvas.h"
#include "include/core/SkSurface.h"
#include "interop.h"

#include <stack>

using namespace std;

class AvgDrawingContext : public ComSingleObject<IAvgDrawingContext, &IID_IAvgDrawingContext>
{
    sk_sp<SkSurface> _surface;
    SkCanvas* _canvas;
    ComPtr<IUnknown> _release;
    SkPaint _fillPaint;
    SkPaint _strokePaint;
    double _scaling;

    double _currentOpacity = 1.0f;
    stack<double> _opacityStack;

    void ConfigurePaint(AvgPen pen);
    void ConfigurePaint(AvgBrush brush);

  public:
    AvgDrawingContext(sk_sp<SkSurface> surface, SkCanvas* canvas, IUnknown* release, double scaling)
        : _surface(surface), _canvas(canvas), _release(release), _scaling(scaling){};
    FORWARD_IUNKNOWN()

    void SetTransform(AvgMatrix3x2* matrix) override;

    void Clear(unsigned int color) override;

    void DrawImage(IAvgImage* image) override;

    void DrawGeometry(IAvgPath* path, AvgBrush brush, AvgPen pen) override;

    void DrawRectangle(AvgRoundRect rect, AvgBrush brush, AvgPen pen, AvgBoxShadow* boxshadows, int n_boxshadows) override;

    void DrawLine(AvgPoint p1, AvgPoint p2, AvgPen pen) override;

    void DrawGlyphRun(IAvgGlyphRun* glyphRun, double x, double y, AvgBrush brush) override;

    void PushClip(AvgRoundRect clip) override;

    void PopClip() override;

    void PushOpacity(double opacity) override;

    void PopOpacity() override;

    double GetScaling() override;

    ~AvgDrawingContext() override
    {
        _canvas->flush();
    };
};

#endif // AVALONIA_SKIA_AVGDRAWINGCONTEXT_H
