//
//  trayicon.h
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 09/09/2021.
//  Copyright © 2021 Avalonia. All rights reserved.
//

#ifndef trayicon_h
#define trayicon_h

#include "common.h"

class AvnTrayIcon : public ComSingleObject<IAvnTrayIcon, &IID_IAvnTrayIcon>
{
private:
    NSStatusItem* _native;
    ComPtr<IAvnTrayIconEvents> _events;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnTrayIcon(IAvnTrayIconEvents* events);
    
    virtual HRESULT SetIcon (void* data, size_t length) override;
    
    virtual HRESULT SetMenu (IAvnMenu* menu) override;
};

#endif /* trayicon_h */
