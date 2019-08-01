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
#include "IGetNative.h"

class AvnAppMenuItem;

@interface AvnMenu : NSMenu // for some reason it doesnt detect nsmenu here but compiler doesnt complain

@end

@interface AvnMenuItem : NSMenuItem
- (id) initWithAvnAppMenuItem: (AvnAppMenuItem*)menuItem;
- (void)didSelectItem:(id)sender;
@end

class AvnAppMenuItem : public ComSingleObject<IAvnAppMenuItem, &IID_IAvnAppMenuItem>, public IGetNative
{
private:
    AvnMenuItem* _native; // here we hold a pointer to an AvnMenuItem
    IAvnActionCallback* _callback;
    IAvnPredicateCallback* _predicate;
    
public:
    FORWARD_IUNKNOWN()
    
    AvnAppMenuItem()
    {
        _native = [[AvnMenuItem alloc] initWithAvnAppMenuItem: this];
        _callback = nullptr;
    }
    
    void* GetNative() override
    {
        return (__bridge void*) _native;
    }
    
    virtual HRESULT SetSubMenu (IAvnAppMenu* menu) override
    {
        auto nsMenu = (__bridge AvnMenu*) dynamic_cast<IGetNative*>(menu)->GetNative();
        
        [_native setSubmenu: nsMenu];
        
        return S_OK;
    }
    
    virtual HRESULT SetTitle (void* utf8String) override
    {
        [_native setTitle:[NSString stringWithUTF8String:(const char*)utf8String]];
        
        return S_OK;
    }
    
    virtual HRESULT SetGesture (void* utf8String) override
    {
        return S_OK;
    }
    
    virtual HRESULT SetAction (IAvnPredicateCallback* predicate, IAvnActionCallback* callback) override
    {
        _predicate = predicate;
        _callback = callback;
        return S_OK;
    }
    
    bool EvaluateItemEnabled()
    {
        if(_predicate != nullptr)
        {
            auto result = _predicate->Evaluate ();
            
            return result;
        }
        
        return false;
    }
    
    void RaiseOnClicked()
    {
        if(_callback != nullptr)
        {
            _callback->Run();
        }
    }
};

#endif

