#include "common.h"
#include "AvnString.h"

class Clipboard : public ComSingleObject<IAvnClipboard, &IID_IAvnClipboard>
{
private:
    NSPasteboard* _pb;
    NSPasteboardItem* _item;
public:
    FORWARD_IUNKNOWN()
    
    Clipboard(NSPasteboard* pasteboard, NSPasteboardItem* item)
    {
        if(pasteboard == nil && item == nil)
            pasteboard = [NSPasteboard generalPasteboard];

        _pb = pasteboard;
        _item = item;
    }
    
    NSPasteboardItem* TryGetItem()
    {
        return _item;
    }
   
    virtual HRESULT GetText (char* type, IAvnString**ppv) override
    {
        @autoreleasepool
        {
            if(ppv == nullptr)
            {
                return E_POINTER;
            }
            NSString* typeString = [NSString stringWithUTF8String:(const char*)type];
            NSString* string = _item == nil ? [_pb stringForType:typeString] : [_item stringForType:typeString];
            
            *ppv = CreateAvnString(string);
            
            return S_OK;
        }
    }
    
    virtual HRESULT GetStrings(char* type, IAvnStringArray**ppv) override
    {
        @autoreleasepool
        {
            *ppv= nil;
            NSString* typeString = [NSString stringWithUTF8String:(const char*)type];
            NSObject* data = _item == nil ? [_pb propertyListForType: typeString] : [_item propertyListForType: typeString];
            if(data == nil)
                return S_OK;
            
            if([data isKindOfClass: [NSString class]])
            {
                *ppv = CreateAvnStringArray((NSString*) data);
                return S_OK;
            }
            
            NSArray* arr = (NSArray*)data;
            
            for(int c = 0; c < [arr count]; c++)
                if(![[arr objectAtIndex:c] isKindOfClass:[NSString class]])
                    return E_INVALIDARG;
            
            *ppv = CreateAvnStringArray(arr);
            return S_OK;
        }
    }
    
    virtual HRESULT SetText (char* type, void* utf8String) override
    {
        Clear();
        @autoreleasepool
        {
            auto string = [NSString stringWithUTF8String:(const char*)utf8String];
            auto typeString = [NSString stringWithUTF8String:(const char*)type];
            if(_item == nil)
                [_pb setString: string forType: typeString];
            else
                [_item setString: string forType:typeString];
        }
        
        return S_OK;
    }

    virtual HRESULT Clear() override
    {
        @autoreleasepool
        {
            if(_item != nil)
                _item = [NSPasteboardItem new];
            else
            {
                [_pb clearContents];
                [_pb setString:@"" forType:NSPasteboardTypeString];
            }
        }
        
        return S_OK;
    }
    
    virtual HRESULT ObtainFormats(IAvnStringArray** ppv) override
    {
        *ppv = CreateAvnStringArray(_item == nil ? [_pb types] : [_item types]);
        return S_OK;
    }
};

extern IAvnClipboard* CreateClipboard(NSPasteboard* pb, NSPasteboardItem* item)
{
    return new Clipboard(pb, item);
}

extern NSPasteboardItem* TryGetPasteboardItem(IAvnClipboard*cb)
{
    auto clipboard = dynamic_cast<Clipboard*>(cb);
    if(clipboard == nil)
        return nil;
    return clipboard->TryGetItem();
}
