//
//  AvnString.m
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 07/11/2018.
//  Copyright © 2018 Avalonia. All rights reserved.
//

#include "common.h"
#include <vector>

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
    
    AvnStringImpl(void*ptr, int len)
    {
        _length = len;
        _cstring = (const char*)malloc(_length);
        memcpy((void*)_cstring, ptr, len);
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
        @autoreleasepool
        {
            if(retOut == nullptr)
            {
                return E_POINTER;
            }
            
            *retOut = _length;
            
            return S_OK;
        }
    }
};

class AvnStringArrayImpl : public virtual ComSingleObject<IAvnStringArray, &IID_IAvnStringArray>
{
private:
    std::vector<ComPtr<AvnStringImpl>> _list;
public:
    FORWARD_IUNKNOWN()
    AvnStringArrayImpl(NSArray<NSString*>* array)
    {
        for(int c = 0; c < [array count]; c++)
        {
            _list.push_back(comnew<AvnStringImpl>([array objectAtIndex:c]));
        }
    }
    
    AvnStringArrayImpl(NSArray<NSURL*>* array)
    {
        for(int c = 0; c < [array count]; c++)
        {
            auto s = comnew<AvnStringImpl>([array objectAtIndex:c].absoluteString);
            _list.push_back(s);
        }
    }
    
    AvnStringArrayImpl(NSString* string)
    {
        _list.push_back(comnew<AvnStringImpl>(string));
    }
    
    virtual unsigned int GetCount() override
    {
        return (unsigned int)_list.size();
    }
    
    virtual HRESULT Get(unsigned int index, ComPtr<IAvnString>& ppv) override
    {
        if(_list.size() <= index)
            return E_INVALIDARG;
        ppv = _list[index];
        return S_OK;
    }
};

ComPtr<IAvnString> CreateAvnString(NSString* string)
{
    return comnew<AvnStringImpl>(string);
}

ComPtr<IAvnStringArray> CreateAvnStringArray(NSArray<NSString*> * array)
{
    return comnew<AvnStringArrayImpl>(array);
}

ComPtr<IAvnStringArray> CreateAvnStringArray(NSArray<NSURL*> * array)
{
    return comnew<AvnStringArrayImpl>(array);
}

ComPtr<IAvnStringArray> CreateAvnStringArray(NSString* string)
{
    return comnew<AvnStringArrayImpl>(string);
}

ComPtr<IAvnString> CreateByteArray(void* data, int len)
{
    return comnew<AvnStringImpl>(data, len);
}

NSString* GetNSString(const ComPtr<IAvnString>& s)
{
    NSString* result = nil;
    
    if (s != nullptr)
    {
        char* p;
        if (s->Pointer((void**)&p) == S_OK && p != nullptr)
            result = [NSString stringWithUTF8String:p];
    }
    
    return result;
}

NSArray<NSString*>* GetNSArrayOfStrings(const ComPtr<IAvnStringArray>& array)
{
    auto output = [NSMutableArray array];
    if (array)
    {
        for (int i = 0; i < array->GetCount(); i++)
        {
            ComPtr<IAvnString> arrayItem;
            if (array->Get(i, arrayItem) == 0) {
                NSString* ext = GetNSString(arrayItem);
                [output addObject:ext];
            }
        }
    }
    return output;
}
