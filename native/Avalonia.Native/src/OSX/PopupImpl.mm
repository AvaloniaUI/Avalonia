//
// Created by Dan Walmsley on 06/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#include "WindowInterfaces.h"
#include "AvnView.h"
#include "WindowImpl.h"
#include "automation.h"
#include "menu.h"
#include "common.h"
#import "WindowBaseImpl.h"
#import "WindowProtocol.h"
#import <AppKit/AppKit.h>
#include "PopupImpl.h"

class PopupImpl : public virtual WindowBaseImpl, public IAvnPopup
{
private:
    BEGIN_INTERFACE_MAP()
    INHERIT_INTERFACE_MAP(WindowBaseImpl)
    INTERFACE_MAP_ENTRY(IAvnPopup, IID_IAvnPopup)
    END_INTERFACE_MAP()
    virtual ~PopupImpl(){}
    ComPtr<IAvnWindowEvents> WindowEvents;
    PopupImpl(IAvnWindowEvents* events, IAvnGlContext* gl) : WindowBaseImpl(events, gl)
    {
        WindowEvents = events;
        [Window setLevel:NSPopUpMenuWindowLevel];
    }
protected:
    virtual NSWindowStyleMask GetStyle() override
    {
        return NSWindowStyleMaskBorderless;
    }

    virtual HRESULT Resize(double x, double y, AvnPlatformResizeReason reason) override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            if (Window != nullptr)
            {
                [Window setContentSize:NSSize{x, y}];

                [Window setFrameTopLeftPoint:ToNSPoint(ConvertPointY(lastPositionSet))];
            }

            return S_OK;
        }
    }
public:
    virtual bool ShouldTakeFocusOnShow() override
    {
        return false;
    }
};


extern IAvnPopup* CreateAvnPopup(IAvnWindowEvents*events, IAvnGlContext* gl)
{
    @autoreleasepool
    {
        IAvnPopup* ptr = dynamic_cast<IAvnPopup*>(new PopupImpl(events, gl));
        return ptr;
    }
}