//
//  AvnString.m
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 07/11/2018.
//  Copyright © 2018 Avalonia. All rights reserved.
//

#include "common.h"

class AvnStringImpl : public virtual ComSingleObject<IAvnString, &IID_IAvnString>
{
private:
    int _length;
    const char* _cstring;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnStringImpl(NSString* string)
    { 
        auto cstring = [string cStringUsingEncoding:NSUTF8StringEncoding];
        _length = (int)[string lengthOfBytesUsingEncoding:NSUTF8StringEncoding];
        
        _cstring = (const char*)malloc(_length + 5);
        
        memset((void*)_cstring, 0, _length + 5);
        memcpy((void*)_cstring, (void*)cstring, _length);
    }
    
    virtual ~AvnStringImpl()
    {
        free((void*)_cstring);
    }
    
    virtual HRESULT Pointer(void**retOut) override
    {
        @autoreleasepool
        {
            if(retOut == nullptr)
            {
                return E_POINTER;
            }
            
            *retOut = (void*)_cstring;
            
            return S_OK;
        }
    }
    
    virtual HRESULT Length(int*retOut) override
    {
        if(retOut == nullptr)
        {
            return E_POINTER;
        }
        
        *retOut = _length;
        
        return S_OK;
    }
};

IAvnString* CreateAvnString(NSString* string)
{
    return new AvnStringImpl(string);
}
