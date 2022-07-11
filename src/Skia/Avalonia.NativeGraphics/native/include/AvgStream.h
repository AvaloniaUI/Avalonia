#ifndef AVALONIA_SKIA_AVGSTREAM_H
#define AVALONIA_SKIA_AVGSTREAM_H

#include "comimpl.h"
#include "interop.h"

#include "AvgImage.h"

#include "include/core/SkStream.h"
#include "include/core/SkData.h"

class AvgStream : public ComSingleObject<IAvgStream, &IID_IAvgStream>
{
    ComPtr<IUnknown> _release;
    SkDynamicMemoryWStream _wstream;

    sk_sp<SkData> _data;

  public:
    FORWARD_IUNKNOWN();
    AvgStream();
    ~AvgStream();

    bool write(void* buffer, unsigned int size) override;
    IAvgImage* makeAvgImage() override;
};

#endif
