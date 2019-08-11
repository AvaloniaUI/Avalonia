// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#include "common.h"
#include "AvnString.h"

class Clipboard : public ComSingleObject<IAvnClipboard, &IID_IAvnClipboard>
{
public:
    FORWARD_IUNKNOWN()
    
    Clipboard()
    {
        NSPasteboard *pasteBoard = [NSPasteboard generalPasteboard];
        [pasteBoard stringForType:NSPasteboardTypeString];
    }
    
    virtual HRESULT GetText (IAvnString**ppv) override
    {
        @autoreleasepool
        {
            if(ppv == nullptr)
            {
                return E_POINTER;
            }
            
            *ppv = CreateAvnString([[NSPasteboard generalPasteboard] stringForType:NSPasteboardTypeString]);
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetText (void* utf8String) override
    {
        @autoreleasepool
        {
            NSPasteboard *pasteBoard = [NSPasteboard generalPasteboard];
            [pasteBoard clearContents];
            [pasteBoard setString:[NSString stringWithUTF8String:(const char*)utf8String] forType:NSPasteboardTypeString];
        }
        
        return S_OK;
    }

    virtual HRESULT Clear() override
    {
        @autoreleasepool
        {
            NSPasteboard *pasteBoard = [NSPasteboard generalPasteboard];
            [pasteBoard clearContents];
            [pasteBoard setString:@"" forType:NSPasteboardTypeString];
        }
        
        return S_OK;
    }
};

extern IAvnClipboard* CreateClipboard()
{
    return new Clipboard();
}
