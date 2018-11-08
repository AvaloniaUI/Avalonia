// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#include "common.h"
#include "AvnString.h"

class Clipboard : public ComSingleObject<IAvnClipboard, &IID_IAvnClipboard>
{
public:
    FORWARD_IUNKNOWN()
    virtual IAvnString* GetText () override
    {
        @autoreleasepool
        {
            return CreateAvnString([[NSPasteboard generalPasteboard] stringForType:NSPasteboardTypeString]);
        }
    }
    
    virtual HRESULT SetText (char* text) override
    {
        @autoreleasepool
        {
            NSPasteboard *pasteBoard = [NSPasteboard generalPasteboard];
            [pasteBoard clearContents];
            [pasteBoard setString:@(text) forType:NSPasteboardTypeString];
        }
        
        return S_OK;
    }

    virtual HRESULT Clear() override
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
