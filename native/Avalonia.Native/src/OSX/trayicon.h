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
    bool _isTemplateIcon;

public:
    FORWARD_IUNKNOWN()
    
    AvnTrayIcon();
    
    ~AvnTrayIcon ();
    
    virtual HRESULT SetIcon (void* data, size_t length) override;
    
    virtual HRESULT SetMenu (IAvnMenu* menu) override;
    
    virtual HRESULT SetIsVisible (bool isVisible) override;

    virtual HRESULT SetToolTipText (char* text) override;

    virtual HRESULT SetIsTemplateIcon (bool isTemplateIcon) override;
};

#endif /* trayicon_h */
