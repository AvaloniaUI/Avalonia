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
    NSError* _error = nil;
    BOOL _dataIsStale;
    
public:
    FORWARD_IUNKNOWN()
    
    SandboxBookmarkImpl(NSURL* url)
    {
        _url = url;
        NSError *error = nil;
        _bookmarkData = [url bookmarkDataWithOptions:NSURLBookmarkCreationWithSecurityScope includingResourceValuesForKeys:nil relativeToURL:nil error:&error];
        _dataIsStale = _bookmarkData == nil || _bookmarkData.length == 0;
        _error = error;
    }
    
    SandboxBookmarkImpl(NSData* bookmarkData)
    {
        _bookmarkData = bookmarkData;
        
        NSError *error = nil;
        _url = [NSURL URLByResolvingBookmarkData:bookmarkData options:NSURLBookmarkResolutionWithSecurityScope|NSURLBookmarkResolutionWithoutUI relativeToURL:nil bookmarkDataIsStale:&_dataIsStale error:&error];
        _error = error;
    }
    
    virtual HRESULT GetURL(IAvnString**ppv) override
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
    
    virtual HRESULT GetError(IAvnString**ppv) override
    {
         START_COM_CALL;
         
         @autoreleasepool
         {
             if(_error == nil){
                 *ppv = CreateAvnString(@"");
                 return S_OK;
             }
             
             NSString* string = [_error localizedDescription];
             *ppv = CreateAvnString(string);
             
             return S_OK;
         }
    }
    
    bool GetDataIsStale() override
    {
        START_COM_CALL;
        return _dataIsStale;
    }
    
    void Restore () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            _bookmarkData = [_url bookmarkDataWithOptions:NSURLBookmarkCreationWithSecurityScope includingResourceValuesForKeys:nil relativeToURL:nil error:NULL];
            _dataIsStale = _bookmarkData == nil || _bookmarkData.length == 0;
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
