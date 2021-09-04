//
//  SandboxBookmark.m
//  Avalonia.Native.OSX
//
//  Created by Mikolaytis Sergey on 03.09.2021.
//  Copyright © 2021 Avalonia. All rights reserved.
//

#include "common.h"
#include "SandboxBookmark.h"
#include "AvnString.h"

class SandboxBookmarkImpl : public ComSingleObject<IAvnSandboxBookmark, &IID_IAvnSandboxBookmark>
{
private:
    NSURL* _url;
    NSData* _bookmarkData;
    
public:
    FORWARD_IUNKNOWN()
    
    SandboxBookmarkImpl(NSURL* url)
    {
        _url = url;
        _bookmarkData = [url bookmarkDataWithOptions:NSURLBookmarkCreationWithSecurityScope includingResourceValuesForKeys:nil relativeToURL:nil error:NULL];
    }
    
    SandboxBookmarkImpl(NSData* bookmarkData)
    {
        _bookmarkData = bookmarkData;
        
        BOOL bookmarkDataIsStale;
        _url = [NSURL URLByResolvingBookmarkData:bookmarkData options:NSURLBookmarkResolutionWithSecurityScope|NSURLBookmarkResolutionWithoutUI relativeToURL:nil bookmarkDataIsStale:&bookmarkDataIsStale error:NULL];
    }
    
    virtual HRESULT GetURL (IAvnString**ppv) override
    {
         START_COM_CALL;
         
         @autoreleasepool
         {
             NSString* string = [_url absoluteString];
             *ppv = CreateAvnString(string);
             
             return S_OK;
         }
    }
     
    virtual HRESULT GetBytes(IAvnString**ppv) override
    {
         START_COM_CALL;
         
         @autoreleasepool
         {
             *ppv = CreateByteArray((void*)_bookmarkData.bytes, (int)_bookmarkData.length);
             return S_OK;
         }
    }
    
    
    void Open () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            [_url startAccessingSecurityScopedResource];
        }
    }
    
    void Close () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            [_url stopAccessingSecurityScopedResource];
        }
    }
    
};

IAvnSandboxBookmark* CreateSandboxBookmark(NSURL* url)
{
    return new SandboxBookmarkImpl(url);
}

IAvnSandboxBookmark* CreateSandboxBookmark(NSData* data)
{
    return new SandboxBookmarkImpl(data);
}
