#include "AvgString.h"
#include <stdio.h>

AvgString::AvgString(std::string string)
{
    _string = string;
}

AvgString::~AvgString()
{

}

HRESULT AvgString::Pointer(void **retOut)
{
    *retOut = (void *) _string.c_str();
    return 0;
}

HRESULT AvgString::Length(int* retOut) {
    *retOut = _string.length();
    return 0;
}

