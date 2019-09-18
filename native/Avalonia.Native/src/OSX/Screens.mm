// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#include "common.h"

class Screens : public ComSingleObject<IAvnScreens, &IID_IAvnScreens>
{
    public:
    FORWARD_IUNKNOWN()
    virtual HRESULT GetScreenCount (int* ret) override
    {
        @autoreleasepool
        {
            *ret = (int)[NSScreen screens].count;
            
            return S_OK;
        }
    }
    
    virtual HRESULT GetScreen (int index, AvnScreen* ret) override
    {
        @autoreleasepool
        {
            if(index < 0 || index >= [NSScreen screens].count)
            {
                return E_INVALIDARG;
            }
            
            auto screen = [[NSScreen screens] objectAtIndex:index];
            
            ret->Bounds.X = [screen frame].origin.x;
            ret->Bounds.Y = [screen frame].origin.y;
            ret->Bounds.Height = [screen frame].size.height;
            ret->Bounds.Width = [screen frame].size.width;
            
            ret->WorkingArea.X = [screen visibleFrame].origin.x;
            ret->WorkingArea.Y = [screen visibleFrame].origin.y;
            ret->WorkingArea.Height = [screen visibleFrame].size.height;
            ret->WorkingArea.Width = [screen visibleFrame].size.width;
            
            ret->PixelDensity = [screen backingScaleFactor];
            
            ret->Primary = index == 0;
            
            return S_OK;
        }
    }
};

extern IAvnScreens* CreateScreens()
{
    return new Screens();
}
