//
//  menu.h
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 01/08/2019.
//  Copyright © 2019 Avalonia. All rights reserved.
//

#ifndef menu_h
#define menu_h

#include "common.h"

class AvnAppMenuItem;
class AvnAppMenu;

@interface AvnMenu : NSMenu
- (id) initWithDelegate: (NSObject<NSMenuDelegate>*) del;
- (void) setHasGlobalMenuItem: (bool) value;
- (bool) hasGlobalMenuItem;
@end

@interface AvnMenuItem : NSMenuItem
- (id) initWithAvnAppMenuItem: (AvnAppMenuItem*)menuItem;
- (void)didSelectItem:(id)sender;
@end

class AvnAppMenuItem : public ComSingleObject<IAvnMenuItem, &IID_IAvnMenuItem>
{
private:
    NSMenuItem* _native; // here we hold a pointer to an AvnMenuItem
    IAvnActionCallback* _callback;
    IAvnPredicateCallback* _predicate;
    bool _isSeperator;
    bool _isCheckable;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnAppMenuItem(bool isSeperator);
    
    NSMenuItem* GetNative();
    
    virtual HRESULT SetSubMenu (IAvnMenu* menu) override;
    
    virtual HRESULT SetTitle (void* utf8String) override;
    
    virtual HRESULT SetGesture (void* key, AvnInputModifiers modifiers) override;
    
    virtual HRESULT SetAction (IAvnPredicateCallback* predicate, IAvnActionCallback* callback) override;
    
    virtual HRESULT SetIsChecked (bool isChecked) override;
    
    virtual HRESULT SetToggleType (AvnMenuItemToggleType toggleType) override;
    
    virtual HRESULT SetIcon (void* data, size_t length) override;
    
    bool EvaluateItemEnabled();
    
    void RaiseOnClicked();
};


class AvnAppMenu : public ComSingleObject<IAvnMenu, &IID_IAvnMenu>
{
private:
    AvnMenu* _native;
    ComPtr<IAvnMenuEvents> _baseEvents;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnAppMenu(IAvnMenuEvents* events);
        
    AvnMenu* GetNative();
    
    void RaiseNeedsUpdate ();
    
    virtual HRESULT InsertItem (int index, IAvnMenuItem* item) override;
    
    virtual HRESULT RemoveItem (IAvnMenuItem* item) override;
    
    virtual HRESULT SetTitle (void* utf8String) override;
    
    virtual HRESULT Clear () override;
};


@interface AvnMenuDelegate : NSObject<NSMenuDelegate>
- (id) initWithParent: (AvnAppMenu*) parent;
@end

#endif

