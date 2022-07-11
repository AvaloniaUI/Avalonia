#include <stdio.h>
#include "AvgStream.h"

AvgStream::AvgStream()
{

}

AvgStream::~AvgStream()
{

}

bool AvgStream::write(void* buffer, unsigned int size)
{
    _wstream.write(buffer, size);
    return 0;
}


IAvgImage* AvgStream::makeAvgImage()
{
    _data = _wstream.detachAsData();

    return new AvgImage(_data);
}

