//
// Created by kekekeks on 04.05.22.
//

#include "AvgDrawingContext.h"
#include "AvgPath.h"
#include "AvgImage.h"
#include "AvgGlyphRun.h"
#include "comimpl.h"
#include "include/core/SkRRect.h"
#include "interop.h"

void AvgDrawingContext::SetTransform(AvgMatrix3x2* matrix)
{
    SkMatrix skMatrix;
    skMatrix.setAll((float)matrix->M11, (float)matrix->M21, (float)matrix->M31, (float)matrix->M12, (float)matrix->M22,
                    (float)matrix->M32, 0, 0, 1);
    _canvas->setMatrix(skMatrix);
}

void AvgDrawingContext::Clear(unsigned int color)
{
    _canvas->clear(color);
}

void AvgDrawingContext::DrawImage(IAvgImage* image)
{
    AvgImage* avgImage = dynamic_cast<AvgImage*>(image);

    _canvas->drawImage(avgImage->_image, 0, 0);
}

void AvgDrawingContext::ConfigurePaint(AvgBrush brush)
{
    double opacity = brush.Opacity * _currentOpacity;

    SkColor color = SkColorSetARGB(((double)(brush.Color.A) * opacity), brush.Color.R, brush.Color.G, brush.Color.B);
    _fillPaint.reset();
    _fillPaint.setColor(color);
    _fillPaint.setAntiAlias(true);
}

void AvgDrawingContext::ConfigurePaint(AvgPen pen)
{
    _strokePaint.reset();
    if (pen.Thickness == 0.0)
    {
        return;
    }

    AvgBrush& brush = pen.Brush;

    double opacity = brush.Opacity * _currentOpacity;
    SkColor color = SkColorSetARGB(((double)(brush.Color.A) * opacity), brush.Color.R, brush.Color.G, brush.Color.B);
    _strokePaint.setColor(color);
    _strokePaint.setAntiAlias(true);
    _strokePaint.setStroke(true);
    _strokePaint.setStrokeWidth(pen.Thickness);
}

void AvgDrawingContext::DrawGeometry(IAvgPath* path, AvgBrush brush, AvgPen pen)
{
    AvgPath* avgPath = dynamic_cast<AvgPath*>(path);

    if (brush.Valid)
    {
        ConfigurePaint(brush);
        _canvas->drawPath(avgPath->_path, _fillPaint);
    }

    if (pen.Valid)
    {
        ConfigurePaint(pen);
        _canvas->drawPath(avgPath->_path, _strokePaint);
    }
}

void AvgDrawingContext::DrawRectangle(AvgRoundRect rrect, AvgBrush brush, AvgPen pen, AvgBoxShadow* boxshadows,
                                      int n_boxshadows)
{
    AvgRect rect = rrect.Rect;
    SkRect skRect = SkRect::MakeXYWH(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
    SkRRect skRRect;

    SkVector corners[] = {
        {(float)rrect.RadiiTopLeft.X, (float)rrect.RadiiTopLeft.Y},
        {(float)rrect.RadiiTopRight.X, (float)rrect.RadiiTopRight.Y},
        {(float)rrect.RadiiBottomRight.X, (float)rrect.RadiiBottomRight.Y},
        {(float)rrect.RadiiBottomLeft.X, (float)rrect.RadiiBottomLeft.Y},
    };

    if (rrect.IsRounded)
    {
        skRRect.setRectRadii(skRect, corners);
    }

    for (int i = 0; i < n_boxshadows; i++)
    {
        printf("Point %d: {%f,%f}\n", i, boxshadows[i].OffsetX, boxshadows[i].OffsetY);
    }

    if (brush.Valid)
    {
        ConfigurePaint(brush);
        if (rrect.IsRounded)
        {
            _canvas->drawRRect(skRRect, _fillPaint);
        }
        else
        {
            _canvas->drawRect(skRect, _fillPaint);
        }
    }

    if (pen.Valid)
    {
        ConfigurePaint(pen);
        if (rrect.IsRounded)
        {
            _canvas->drawRRect(skRRect, _strokePaint);
        }
        else
        {
            _canvas->drawRect(skRect, _strokePaint);
        }
    }
}

void AvgDrawingContext::DrawLine(AvgPoint p1, AvgPoint p2, AvgPen pen)
{
    if (pen.Valid)
    {
        ConfigurePaint(pen);
        _canvas->drawLine(p1.X, p1.Y, p2.X, p2.Y, _strokePaint);
    }
}

void AvgDrawingContext::DrawGlyphRun(IAvgGlyphRun* glyphRun, double x, double y, AvgBrush brush)
{
    AvgGlyphRun* avgGlyphRun = dynamic_cast<AvgGlyphRun*>(glyphRun);

    ConfigurePaint(brush);

    // printf("Calling drawTextBlob() %f,%f : %f:%f\n", x,y, avgGlyphRun->_textBlob.get()->bounds().x(), avgGlyphRun->_textBlob.get()->bounds().y());

    _canvas->drawTextBlob(avgGlyphRun->_textBlob, x, y, _fillPaint);
    // _canvas->drawSimpleText("hello world", 10, SkTextEncoding::kUTF8, x,y, avgGlyphRun->_fontManager->_font, _fillPaint);
}

void AvgDrawingContext::PushOpacity(double opacity)
{
    _opacityStack.push(_currentOpacity);
    _currentOpacity *= opacity;
}

void AvgDrawingContext::PopOpacity()
{
    _currentOpacity = _opacityStack.top();
    _opacityStack.pop();
}

void AvgDrawingContext::PushClip(AvgRoundRect clip)
{
    AvgRect rect = clip.Rect;
    SkRect skRect = {(float)rect.X, (float)rect.Y, (float)(rect.X + rect.Width), (float)(rect.Y + rect.Height)};
    SkRRect skRRect;
    SkVector corners[] = {
        {(float)clip.RadiiTopLeft.X, (float)clip.RadiiTopLeft.Y},
        {(float)clip.RadiiTopRight.X, (float)clip.RadiiTopRight.Y},
        {(float)clip.RadiiBottomRight.X, (float)clip.RadiiBottomRight.Y},
        {(float)clip.RadiiBottomLeft.X, (float)clip.RadiiBottomLeft.Y},
    };

    _canvas->save();

    if (clip.IsRounded)
    {
        skRRect.setRectRadii(skRect, corners);
        _canvas->clipRRect(skRRect);
    }
    else
    {
        _canvas->clipRect(skRect);
    }
}

void AvgDrawingContext::PopClip()
{
    _canvas->restore();
}

double AvgDrawingContext::GetScaling()
{
    return _scaling;
}
