//
// Created by kekekeks on 04.05.22.
//

#include "AvgDrawingContext.h"

void AvgDrawingContext::SetTransform(AvgMatrix3x2 *matrix) {
    SkMatrix skMatrix;
    skMatrix.setAll((float)matrix->M11,(float)matrix->M21,(float)matrix->M31,
                      (float)matrix->M12, (float)matrix->M22, (float)matrix->M32,
                      0,0,1);
    _canvas->setMatrix(skMatrix);
}

void AvgDrawingContext::Clear(unsigned int color) {
    _canvas->clear(color);
}

void AvgDrawingContext::FillRect(AvgRect rect, unsigned int color) {

    SkRect skRect = {(float)rect.X, (float)rect.Y, (float)(rect.X + rect.Width), (float)(rect.Y+rect.Height)};
    SkPaint paint;
    paint.setColor(color);
    paint.setStroke(false);
    _canvas->drawRect(skRect, paint);
}
