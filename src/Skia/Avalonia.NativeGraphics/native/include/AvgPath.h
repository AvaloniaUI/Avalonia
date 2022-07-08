//
// Created by jameswalmsley on 08.05.22
//

#ifndef AVALONIA_SKIA_AVGPATH_H
#define AVALONIA_SKIA_AVGPATH_H

#include "comimpl.h"
#include "interop.h"
#include "include/core/SkPath.h"

class AvgPath : public ComSingleObject<IAvgPath, &IID_IAvgPath>
{
    ComPtr<IUnknown> _release;

  public:
    SkPath _path;
    FORWARD_IUNKNOWN()
    AvgPath();
    ~AvgPath();

    void ArcTo(AvgPoint point, AvgSize size, double rotationAngle, bool isLargeArc, AvgSweepDirection sweepDirection) override;
    void BeginFigure(AvgPoint startPoint, bool isFilled) override;
    void CubicBezierTo(AvgPoint p1, AvgPoint p2, AvgPoint p3) override;
    void QuadraticBezierTo(AvgPoint p1, AvgPoint p2) override;
    void LineTo(AvgPoint p1) override;
    void EndFigure(bool isClosed) override;
    void SetFillRule(AvgFillRule fillRule) override;
    void AddRect(AvgRect rect) override;
    void MoveTo(AvgPoint point) override;
    void AddOval(AvgRect rect) override;
};

#endif
