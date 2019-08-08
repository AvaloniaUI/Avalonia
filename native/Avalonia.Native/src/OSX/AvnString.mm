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
    NSString* _string;
    const char* _cstring;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnStringImpl(NSString* string)
    {
        _string = string;
        _cstring = [_string cStringUsingEncoding:NSUTF8StringEncoding];
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
        
        *retOut = (int)[_string lengthOfBytesUsingEncoding:NSUTF8StringEncoding];
        
        return S_OK;
    }
};

IAvnString* CreateAvnString(NSString* string)
{
    return new AvnStringImpl(string);
}
