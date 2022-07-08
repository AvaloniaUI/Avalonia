#ifndef AVALONIA_SKIA_AVGSTRING_H
#define AVALONIA_SKIA_AVGSTRING_H

#include "comimpl.h"
#include "interop.h"
#include <string>

class AvgString : public ComSingleObject<IAvgString, &IID_IAvgString>
{
  public:
        FORWARD_IUNKNOWN()
        std::string _string;
        AvgString(std::string string);
        ~AvgString();

        HRESULT Pointer(void**retOut) override;
        HRESULT Length(int*retOut) override;
};

#endif
