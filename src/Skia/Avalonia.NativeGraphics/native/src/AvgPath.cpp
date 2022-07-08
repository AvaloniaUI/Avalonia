#include <stdio.h>
#include "include/core/SkPath.h"
#include "AvgPath.h"

AvgPath::AvgPath()
{
}

AvgPath::~AvgPath()
{
}

void AvgPath::ArcTo(AvgPoint point, AvgSize size, double rotationAngle, bool isLargeArc, AvgSweepDirection sweepDirection)
{
    _path.arcTo(size.Width, size.Height, rotationAngle, isLargeArc ? SkPath::ArcSize::kLarge_ArcSize : SkPath::ArcSize::kSmall_ArcSize, 
                sweepDirection == AvgSweepDirection::ClockWise ? SkPathDirection::kCW : SkPathDirection::kCCW,
                point.X,
                point.Y);
}

void AvgPath::BeginFigure(AvgPoint startPoint, bool isFilled)
{
    _path.moveTo(startPoint.X, startPoint.Y);
    
}

void AvgPath::CubicBezierTo(AvgPoint p1, AvgPoint p2, AvgPoint p3)
{
    _path.cubicTo(p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y);
}

void AvgPath::EndFigure(bool isClosed)
{
    if(isClosed)
    {
        _path.close();
    }
}

void AvgPath::QuadraticBezierTo(AvgPoint p1, AvgPoint p2)
{
    _path.quadTo(p1.X, p1.Y, p2.X, p2.Y);
}

void AvgPath::LineTo(AvgPoint point)
{
    _path.lineTo(point.X, point.Y);
}

void AvgPath::SetFillRule(AvgFillRule fillRule)
{
    _path.setFillType(fillRule == AvgFillRule::EvenOdd ? SkPathFillType::kEvenOdd : SkPathFillType::kWinding);
}

void AvgPath::AddRect(AvgRect rect)
{
    _path.addRect(SkRect::MakeXYWH(rect.X, rect.Y, rect.Width, rect.Height));
}

void AvgPath::MoveTo(AvgPoint point)
{
    _path.moveTo(SkPoint::Make(point.X, point.Y));
}

void AvgPath::AddOval(AvgRect rect)
{
    _path.addRect(SkRect::MakeXYWH(rect.X, rect.Y, rect.Width, rect.Height));
}
