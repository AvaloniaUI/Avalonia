// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#include "common.h"

class Clipboard : public ComSingleObject<IAvnClipboard, &IID_IAvnClipboard>
{
public:
    FORWARD_IUNKNOWN()
    virtual HRESULT GetText (void** retOut)
    {
        @autoreleasepool
        {
            NSString *str = [[NSPasteboard generalPasteboard] stringForType:NSPasteboardTypeString];
            *retOut = (void *)str.UTF8String;
        }
        
        return S_OK;
    }
    
    virtual HRESULT SetText (char* text)
    {
        @autoreleasepool
        {
            NSPasteboard *pasteBoard = [NSPasteboard generalPasteboard];
            [pasteBoard clearContents];
            [pasteBoard setString:@(text) forType:NSPasteboardTypeString];
        }
        
        return S_OK;
    }

    virtual HRESULT Clear()
    {
        @autoreleasepool
        {
            [[NSPasteboard generalPasteboard] clearContents];
        }
        
        return S_OK;
    }
};

extern IAvnClipboard* CreateClipboard()
{
    return new Clipboard();
}
