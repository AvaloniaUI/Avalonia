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

@interface AvnMenu : NSMenu // for some reason it doesnt detect nsmenu here but compiler doesnt complain
- (void)setMenu:(NSMenu*) menu;
@end

@interface AvnMenuItem : NSMenuItem
- (id) initWithAvnAppMenuItem: (AvnAppMenuItem*)menuItem;
- (void)didSelectItem:(id)sender;
@end

class AvnAppMenuItem : public ComSingleObject<IAvnAppMenuItem, &IID_IAvnAppMenuItem>
{
private:
    NSMenuItem* _native; // here we hold a pointer to an AvnMenuItem
    IAvnActionCallback* _callback;
    IAvnPredicateCallback* _predicate;
    bool _isSeperator;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnAppMenuItem(bool isSeperator);
    
    NSMenuItem* GetNative();
    
    virtual HRESULT SetSubMenu (IAvnAppMenu* menu) override;
    
    virtual HRESULT SetTitle (void* utf8String) override;
    
    virtual HRESULT SetGesture (void* key, AvnInputModifiers modifiers) override;
    
    virtual HRESULT SetAction (IAvnPredicateCallback* predicate, IAvnActionCallback* callback) override;
    
    bool EvaluateItemEnabled();
    
    void RaiseOnClicked();
};


class AvnAppMenu : public ComSingleObject<IAvnAppMenu, &IID_IAvnAppMenu>
{
private:
    AvnMenu* _native;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnAppMenu();
    
    AvnAppMenu(AvnMenu* native);
    
    AvnMenu* GetNative();
    
    virtual HRESULT AddItem (IAvnAppMenuItem* item) override;
    
    virtual HRESULT RemoveItem (IAvnAppMenuItem* item) override;
    
    virtual HRESULT SetTitle (void* utf8String) override;
    
    virtual HRESULT Clear () override;
};


#endif

