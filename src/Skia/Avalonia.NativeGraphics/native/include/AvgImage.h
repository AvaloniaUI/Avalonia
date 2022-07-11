#ifndef AVALONIA_SKIA_AVGIMAGE_H
#define AVALONIA_SKIA_AVGIMAGE_H

#include "comimpl.h"
#include "interop.h"

#include "include/core/SkData.h"
#include "include/core/SkImage.h"

class AvgImage : public ComSingleObject<IAvgImage, &IID_IAvgImage>
{
    ComPtr<IUnknown> _release;


  public:
    FORWARD_IUNKNOWN();
    sk_sp<SkImage> _image;
    AvgImage(sk_sp<SkData> data);
    ~AvgImage();

    AvgVector getSize() override;
};

#endif
