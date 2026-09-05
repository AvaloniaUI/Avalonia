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

class AvnTrayIcon;

@interface AvnStatusItem : NSObject
@property (nonatomic, strong, readonly) NSStatusItem* statusItem;

- (instancetype) initWithOwner: (AvnTrayIcon*) owner;
- (void) clicked: (id) sender;
- (void) dispose;
@end

class AvnTrayIcon : public ComSingleObject<IAvnTrayIcon, &IID_IAvnTrayIcon>
{
private:
    NSMenu* _menu;
    bool _isTemplateIcon;
    AvnStatusItem* _native;
    ComPtr<IAvnActionCallback> _clickedCallback;

public:
    FORWARD_IUNKNOWN()

    AvnTrayIcon();

    ~AvnTrayIcon ();

    virtual HRESULT SetIcon (void* data, size_t length) override;

    virtual HRESULT SetMenu (IAvnMenu* menu) override;

    virtual HRESULT SetIsVisible (bool isVisible) override;

    virtual HRESULT SetToolTipText (char* text) override;

    virtual HRESULT SetIsTemplateIcon (bool isTemplateIcon) override;

    virtual HRESULT SetClickedCallback (IAvnActionCallback* callback) override;

    void OnClicked(NSEvent* event);
};

#endif /* trayicon_h */
